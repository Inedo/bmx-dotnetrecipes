using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Gadgets;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Extensibility.Scripting;
using Inedo.BuildMaster.Web;
using Inedo.BuildMaster.Web.Security;
using Inedo.BuildMasterExtensions.DotNetRecipes.Gadgets;
using Inedo.BuildMasterExtensions.DotNetRecipes.Providers;
using Inedo.Web.Handlers;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    [RecipeProperties(
        "BitChecker",
        "Creates a new application that pulls code from a sample repository, compiles the project, and then publishes the files to local directories.",
        RecipeScopes.Example)]
    [CustomEditor(typeof(ExampleAspNetRecipeEditor))]
    [WorkflowCreatingRequirements(MinimumEnvironmentCount = 1)]
    public sealed class ExampleAspNetRecipe : RecipeBase, IApplicationCreatingRecipe, IWorkflowCreatingRecipe, IScmCreatingRecipe, IDashboardCreatingRecipe
    {
        public string DeploymentPath { get; set; }
        public string ApplicationName { get; set; }
        public string ApplicationGroup { get; set; }
        public int ApplicationId { get; set; }
        public string WorkflowName { get; set; }
        public int[] WorkflowSteps { get; set; }
        public int WorkflowId { get; set; }
        public string SourceControlProviderName
        {
            get { return "Example"; }
            set { throw new NotSupportedException(); }
        }
        public int ScmProviderId { get; set; }
        public string ScmPath { get; set; }

        public SourceControlProviderBase InstantiateSourceControlProvider()
        {
            return new ExampleSourceControlProvider();
        }
        public override void Execute()
        {
            var workflowSteps = StoredProcs
                .Workflows_GetWorkflow(this.WorkflowId)
                .Execute()
                .WorkflowSteps_Extended;

            int deployableId;
            int databaseDeployableId;
            {
                var proc = StoredProcs
                    .Applications_CreateOrUpdateDeployable(
                        null,
                        this.ApplicationId,
                        "Web",
                        Domains.DeployableTypes.Other);
                proc.ExecuteNonQuery();
                deployableId = proc.Deployable_Id.Value;

                proc = StoredProcs
                    .Applications_CreateOrUpdateDeployable(
                        null,
                        this.ApplicationId,
                        "Database",
                        Domains.DeployableTypes.Other);
                proc.ExecuteNonQuery();
                databaseDeployableId = proc.Deployable_Id.Value;
            }

            StoredProcs.Variables_CreateOrUpdateVariableDefinition("PreviousReleaseNumber", null, null, null, this.ApplicationId, null, null, null, null, "1.0", Domains.YN.No).Execute();
            StoredProcs.Variables_CreateOrUpdateVariableDefinition("PreviousBuildNumber", null, null, null, this.ApplicationId, null, null, null, null, "1", Domains.YN.No).Execute();

            string releaseNumber = "1.1";
            StoredProcs.Releases_CreateOrUpdateRelease(
                this.ApplicationId,
                releaseNumber,
                this.WorkflowId,
                null,
                null,
                null,
                "<ReleaseDeployables><ReleaseDeployable Deployable_Id=\"" + deployableId.ToString() + "\" InclusionType_Code=\"I\" />" +
                "<ReleaseDeployable Deployable_Id=\"" + databaseDeployableId.ToString() + "\" InclusionType_Code=\"I\" /></ReleaseDeployables>")
                .ExecuteNonQuery();

            int configurationFileId;
            {
                var proc = StoredProcs.ConfigurationFiles_CreateConfigurationFile(null, deployableId, "web_appsettings.config", null, null);
                proc.ExecuteNonQuery();
                configurationFileId = proc.ConfigurationFile_Id.Value;

                StoredProcs.ConfigurationFiles_CreateConfigurationFileInstance(
                    ConfigurationFile_Id: configurationFileId,
                    Instance_Name: "Template",
                    Environment_Id: null,
                    Template_Indicator: Domains.YN.Yes,
                    Template_Instance_Name: null,
                    TransformType_Code: Domains.ConfigurationFileTransformTypeCodes.KeyValuePair
                ).Execute();
            }

            foreach (var step in workflowSteps)
            {
                StoredProcs.ConfigurationFiles_CreateConfigurationFileInstance(
                    ConfigurationFile_Id: configurationFileId,
                    Instance_Name: step.Environment_Name,
                    Environment_Id: step.Environment_Id,
                    Template_Indicator: Domains.YN.No,
                    Template_Instance_Name: "Template",
                    TransformType_Code: null
                ).Execute();
            }

            var utf8 = new UTF8Encoding(false);

            StoredProcs.ConfigurationFiles_CreateConfigurationFileVersions(
                ConfigurationFile_Id: configurationFileId,
                ConfigurationFiles_Xml: new XDocument(
                        new XElement("ConfigFiles",
                            from s in workflowSteps
                            select new XElement("Version",
                                new XAttribute("Instance_Name", s.Environment_Name),
                                new XAttribute("VersionNotes_Text", "Created automatically."),
                                new XAttribute("File_Bytes", Convert.ToBase64String(utf8.GetBytes(string.Format("ContactFormRecipient=contact-form-{0}@example.com\r\nSmtpServer={0}-mail-server", s.Environment_Name.ToLowerInvariant()))))
                            ),
                            new XElement("Version",
                                new XAttribute("Instance_Name", "Template"),
                                new XAttribute("VersionNotes_Text", "Created automatically."),
                                new XAttribute("File_Bytes", Convert.ToBase64String(utf8.GetBytes(Properties.Resources.web_appsettings)))
                            )
                        )
                    ).ToString(SaveOptions.DisableFormatting),
                ReleaseNumbers_Csv: "1.1"
            ).Execute();

            int databaseProviderId;
            {
                var proc = StoredProcs.Providers_CreateOrUpdateProvider(
                    null,
                    Domains.ProviderTypes.Database,
                    1,
                    "Example",
                    "Sample database provider for use only with example applications.",
                    Util.Persistence.SerializeToPersistedObjectXml(new ExampleDatabaseProvider()),
                    Domains.YN.Yes);

                proc.ExecuteNonQuery();
                databaseProviderId = (int)proc.Provider_Id;
            }

            this.CreateApproval();

            CreateScript();

            this.CreateBuildDeploymentPlan();
            for (int i = 0; i < this.WorkflowSteps.Length; i++)
                this.CreateEnvironmentDeploymentPlan(i, databaseProviderId);
        }

        private static int CreateScript()
        {
            var scriptType = (IScriptMetadataReader)Activator.CreateInstance(Type.GetType("Inedo.BuildMasterExtensions.Windows.Scripting.PowerShell.PowerShellScriptType,Windows"));
            var metadata = scriptType.GetScriptMetadata(new StreamReader(new MemoryStream(Properties.Resources.BitChecker_SayHelloScript)));

            var existingScript = StoredProcs.Scripts_GetScripts(((ScriptTypeBase)scriptType).ScriptTypeCode, "Y")
                .Execute()
                .FirstOrDefault(s => s.Script_Name.Equals("Say Hello to Inedo", StringComparison.OrdinalIgnoreCase));

            int scriptId;
            if (existingScript != null)
            {
                scriptId = existingScript.Script_Id;
            }
            else
            {
                scriptId = (int)StoredProcs.Scripts_CreateOrUpdateScript(null, "Say Hello to Inedo", metadata.Description, ((ScriptTypeBase)scriptType).ScriptTypeCode, "Y", "Y").Execute();
                StoredProcs.Scripts_CreateVersion(1, scriptId, Properties.Resources.BitChecker_SayHelloScript).Execute();
                foreach (var param in metadata.Parameters)
                    StoredProcs.Scripts_CreateOrUpdateParameter(scriptId, param.Name, Domains.ScriptParameterTypes.Standard, null, param.Description).Execute();
            }

            return scriptId;
        }
        private void CreateBuildDeploymentPlan()
        {
            var licenseKey = string.Join(
                ",",
                StoredProcs.LicenseKeys_GetLicenseKeys().Execute().Select(k => k.LicenseKey_Text.Remove(5, 27).Insert(5, "..."))
            );

            var reader = XmlReader.Create(
                new StringReader(
                    Replacer.Replace(
                        Properties.Resources.BitChecker_Build_Plan,
                        new Dictionary<string, string>
                        {
                            { "ApplicationId", this.ApplicationId.ToString() },
                            { "LicenseKey", licenseKey }
                        }
                    )
                )
            );

            int buildPlanId = Util.Plans.ImportFromXml(reader);

            StoredProcs.Workflows_SetBuildStep(
                Workflow_Id: this.WorkflowId,
                Build_DeploymentPlan_Id: buildPlanId,
                BuildImporterTemplate_Configuration: null
            ).Execute();
        }
        private void CreateEnvironmentDeploymentPlan(int step, int databaseProviderId)
        {
            var environment = StoredProcs.Environments_GetEnvironment(this.WorkflowSteps[step])
                .Execute()
                .Environments
                .First();

            var reader = XmlReader.Create(
                new StringReader(
                    Replacer.Replace(
                        Properties.Resources.BitChecker_Environment_Plan,
                        new Dictionary<string, string>
                        {
                            { "Environment", environment.Environment_Name },
                            { "DatabaseProviderId", databaseProviderId.ToString() }
                        }
                    )
                )
            );

            int planId = Util.Recipes.CreateDeploymentPlanForWorkflowStep(this.WorkflowId, step + 1);
            Util.Plans.ImportFromXml(planId, reader);
        }
        private void CreateApproval()
        {
            int? directoryProviderId = InedoLib.Util.Int.ParseN(StoredProcs.Configuration_GetValue("CoreEx", "DirectoryProvider", null).Execute());

            if (directoryProviderId != 1)
                return;

            var admin = StoredProcs.Users_GetUsers().Execute().Users.FirstOrDefault(u => u.User_Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            if (admin == null)
                return;

            StoredProcs.Promotions_CreateOrUpdateRequirement(
                Environment_Id: this.WorkflowSteps[0],
                Requirement_Description: "Approved by Admin",
                Workflow_Id: this.WorkflowId,
                Principal_Name: "Admin"
            ).Execute();
        }
        public void CreateDashboards()
        {
            // application dashboard
            {
                StoredProcs.Dashboards_CreateDashboard(this.ApplicationId, null, Domains.DashboardScopes.Application).ExecuteNonQuery();

                string dashboardText;
                using (var stream = typeof(ExampleAspNetRecipe).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.DotNetRecipes.BitCheckerApplicationText.html"))
                using (var reader = new StreamReader(stream))
                {
                    dashboardText = reader
                        .ReadToEnd()
                        .Replace("__CREATE_BUILD_URL__", HttpUtility.JavaScriptStringEncode(DynamicHttpHandling.GetProcessRequestDelegateUrl(CreateBuild) + "?applicationId=" + this.ApplicationId));
                }

                int dashboardId = (int)StoredProcs.Dashboards_GetDashboard(this.ApplicationId, Domains.DashboardScopes.Application).ExecuteDataRow()[TableDefs.Dashboards.Dashboard_Id];
                var freeTextGadget = Util.Recipes.Munging.MungeInstance("Inedo.BuildMaster.Extensibility.Gadgets.FreeTextGadget,BuildMaster.Web.WebApplication", new
                {
                    AllowHtml = true,
                    Text = dashboardText
                });

                AddGadget(dashboardId, 1, freeTextGadget);
                AddGadget(dashboardId, 1, new HideNavigationGadget());
            }

            // build dashboard
            {
                string dashboardText;
                using (var stream = typeof(ExampleAspNetRecipe).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.DotNetRecipes.BitCheckerBuildText.html"))
                using (var reader = new StreamReader(stream))
                {
                    dashboardText = reader.ReadToEnd();
                }

                StoredProcs.Dashboards_CreateDashboard(this.ApplicationId, null, Domains.DashboardScopes.Build).ExecuteNonQuery();
                int dashboardId = (int)StoredProcs.Dashboards_GetDashboard(this.ApplicationId, Domains.DashboardScopes.Build).ExecuteDataRow()[TableDefs.Dashboards.Dashboard_Id];
                var freeTextGadget = Util.Recipes.Munging.MungeInstance("Inedo.BuildMaster.Extensibility.Gadgets.FreeTextGadget,BuildMaster.Web.WebApplication", new
                {
                    AllowHtml = true,
                    Text = dashboardText
                });

                AddGadget(dashboardId, 1, freeTextGadget);
                AddGadget(dashboardId, 1, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildDetailsGadget,BuildMaster.Web.WebApplication")));
                AddGadget(dashboardId, 1, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildPromotionStatusGadget,BuildMaster.Web.WebApplication")));

                AddGadget(dashboardId, 2, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildApprovalsGadget,BuildMaster.Web.WebApplication")));
                AddGadget(dashboardId, 2, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildPromotionsGadget,BuildMaster.Web.WebApplication")));
                AddGadget(dashboardId, 2, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildExecutionsGadget,BuildMaster.Web.WebApplication")));

                AddGadget(dashboardId, 3, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildArtifactsGadget,BuildMaster.Web.WebApplication")));
                AddGadget(dashboardId, 3, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.ReleaseNotesGadget,BuildMaster.Web.WebApplication")));

                AddGadget(dashboardId, 4, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildUnitTestsGadget,BuildMaster.Web.WebApplication")));
                AddGadget(dashboardId, 4, Activator.CreateInstance(Type.GetType("Inedo.BuildMaster.Extensibility.Gadgets.Build.BuildReportsGadget,BuildMaster.Web.WebApplication")));
            }
        }

        private static void AddGadget(int dashboardId, int zone, object gadget)
        {
            StoredProcs.Dashboards_CreateOrUpdateGadget(
                null,
                dashboardId,
                zone,
                null,
                Util.Reflection.GetCustomAttribute<GadgetPropertiesAttribute>(gadget.GetType()).Name,
                gadget.ToString(),
                Util.Persistence.SerializeToPersistedObjectXml(gadget)
            ).ExecuteNonQuery();
        }
        private static void CreateBuild(HttpContext context)
        {
            int applicationId = int.Parse(context.Request.QueryString["applicationId"]);

            if (!WebUserContext.CanPerformTask(SecuredTask.Builds_CreateBuild, applicationId: applicationId))
                throw new SecurityException();

            var release = StoredProcs.Releases_GetReleases(applicationId, Domains.ReleaseStatus.Active, 1).Execute().First();

            var buildNumber = StoredProcs.Builds_CreateBuild(
                Application_Id: applicationId,
                Release_Number: release.Release_Number,
                PromoteBuild_Indicator: Domains.YN.Yes,
                StartExecution_Indicator: Domains.YN.Yes,
                ExecutionStart_Date: null,
                Requested_Build_Number: null,
                BuildVariables_Xml: null,
                PromotionVariables_Xml: null,
                ExecutionVariables_Xml: null,
                Build_Number: null,
                BuildImporter_Configuration: null
            ).Execute();

            context.Response.Redirect(
                string.Format(
                    "/applications/{0}/executions/execution-in-progress?releaseNumber={1}&buildNumber={2}",
                    applicationId,
                    Uri.EscapeDataString(release.Release_Number),
                    Uri.EscapeDataString(buildNumber)
                )
            );
        }
    }
}
