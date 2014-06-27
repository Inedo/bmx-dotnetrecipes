using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Web;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Gadgets;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Extensibility.Scripting;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.DotNetRecipes.Actions;
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
    [WorkflowCreatingRequirements(MinimumEnvironmentCount = 2)]
    public sealed class ExampleAspNetRecipe : RecipeBase, IApplicationCreatingRecipe, IWorkflowCreatingRecipe, IScmCreatingRecipe, IDashboardCreatingRecipe
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleAspNetRecipe"/> class.
        /// </summary>
        public ExampleAspNetRecipe()
        {
        }

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
        public SourceControlProviderBase InstantiateSourceControlProvider()
        {
            return new ExampleSourceControlProvider();
        }
        public int ScmProviderId { get; set; }
        public string ScmPath { get; set; }

        public override void Execute()
        {
            // By this point, an app + workflow + Inedo Scm will exist

            var workflowSteps = StoredProcs
                .Workflows_GetWorkflow(this.WorkflowId)
                .ExecuteDataSet(TableNames.Workflows_Extended, TableNames.WorkflowSteps_Extended)
                .Tables[TableNames.WorkflowSteps_Extended];

            // Create Deployables
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

            // Create Release
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

            // Create Config File
            int configurationFileId;
            {
                var proc = StoredProcs.ConfigurationFiles_CreateConfigurationFile(null, deployableId, "web_appsettings.config");
                proc.ExecuteNonQuery();
                configurationFileId = proc.ConfigurationFile_Id.Value;
            }
            foreach (DataRow dr in workflowSteps.Rows)
            {
                CreateConfigFileInstance(
                    releaseNumber,
                    configurationFileId,
                    dr[TableDefs.WorkflowSteps_Extended.Environment_Name].ToString(),
                    (int)dr[TableDefs.WorkflowSteps_Extended.Environment_Id]
                );
            }

            // Create Database Provider
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

            // Create approval 
            CreateApproval();

            int planId;
            // First Environment
            {
                int environmentId = this.WorkflowSteps[0];
                string environmentName = StoredProcs.Environments_GetEnvironment(environmentId).ExecuteDataRow()[TableDefs.Environments.Environment_Name].ToString();

                // Source
                planId = CreatePlan("Web", environmentId, "Source", Properties.Resources.BitChecker_GetSource);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                        ProviderId = this.ScmProviderId
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                        ProviderId = this.ScmProviderId
                    }));
                AddAction(planId,
                    (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.WindowsSdk.DotNet.WriteAssemblyInfoVersionsAction,WindowsSdk",
                    new
                    {
                        FileMasks = new[] { "*\\AssemblyInfo.cs" },
                        Recursive = true,
                        Version = "$ReleaseNumber.$BuildNumber"
                    }));

                // Compare Source
                planId = CreatePlan("Web", environmentId, "Compare Source", Properties.Resources.BitChecker_CompareSource);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "$PreviousReleaseNumber.$PreviousBuildNumber",
                        ProviderId = this.ScmProviderId,
                        OverriddenTargetDirectory = "PrevSrc"
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                        ProviderId = this.ScmProviderId,
                        OverriddenTargetDirectory = "CurrentSrc"
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Reporting.CompareDirectoriesReportingAction", new
                    {
                        IncludeUnchanged = false,
                        Path1 = "PrevSrc",
                        Path2 = "CurrentSrc",
                        OutputName = "Changes between $PreviousReleaseNumber.$PreviousBuildNumber and $ReleaseNumber.$BuildNumber"
                    }));

                // Build
                planId = CreatePlan("Web", environmentId, "Build", Properties.Resources.BitChecker_Build);
                AddAction(planId,
                    (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.WindowsSdk.MSBuild.BuildMSBuildProjectAction,WindowsSdk",
                    new
                    {
                        IsWebProject = true,
                        ProjectBuildConfiguration = "Debug",
                        ProjectPath = "BitChecker.csproj"
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                    {
                        ArtifactName = "Web"
                    }));

                planId = CreatePlan("Web", environmentId, "Unit Tests", Properties.Resources.BitChecker_UnitTests);
                AddAction(planId, new ExampleUnitTestAction { BitCheckerApplicationId = this.ApplicationId });

                // Deploy Web
                planId = CreatePlan("Web", environmentId, "Deploy Web", Properties.Resources.BitChecker_DeployWeb);

                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                    {
                        ArtifactName = "Web",
                        OverriddenTargetDirectory = Path.Combine(this.DeploymentPath, environmentName)
                    }));

                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Configuration.DeployConfigurationFileAction", new
                    {
                        ConfigurationFileName = "web_appsettings.config",
                        InstanceName = environmentName,
                        OverriddenSourceDirectory = Path.Combine(this.DeploymentPath, environmentName)
                    }));

                // Deploy Database
                planId = CreatePlan("Database", environmentId, "Deploy Database", Properties.Resources.BitChecker_DeployDatabase);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Database.ExecuteDatabaseChangeScriptsAction", new
                    {
                        ProviderId = databaseProviderId
                    }));

                // Say Hello to Inedo
                planId = CreatePlan("Web", environmentId, "Say Hello to Inedo", Properties.Resources.BitChecker_SayHello);

                var scriptType = (IScriptMetadataReader)Activator.CreateInstance(Type.GetType("Inedo.BuildMasterExtensions.Windows.Scripting.PowerShell.PowerShellScriptType,Windows"));
                var metadata = scriptType.GetScriptMetadata(new StreamReader(new MemoryStream(Properties.Resources.BitChecker_SayHelloScript)));

                var existingScript = StoredProcs.Scripts_GetScripts(((ScriptTypeBase)scriptType).ScriptTypeCode, "Y")
                    .Execute()
                    .FirstOrDefault(s => s.Script_Name.Equals("Say Hello to Inedo", StringComparison.OrdinalIgnoreCase));

                int? scriptId;
                if (existingScript != null)
                {
                    scriptId = existingScript.Script_Id;
                }
                else
                {
                    scriptId = StoredProcs.Scripts_CreateOrUpdateScript(null, "Say Hello to Inedo", metadata.Description, ((ScriptTypeBase)scriptType).ScriptTypeCode, "Y", "Y").Execute();
                    StoredProcs.Scripts_CreateVersion(1, scriptId, Properties.Resources.BitChecker_SayHelloScript).Execute();
                    foreach (var param in metadata.Parameters)
                        StoredProcs.Scripts_CreateOrUpdateParameter(scriptId, param.Name, Domains.ScriptParameterTypes.Standard, null, param.Description).Execute();
                }

                AddAction(planId,
                    (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.Windows.Shell.ExecutePowerShellScriptAction,Windows",
                    new
                    {
                        ScriptId = scriptId,
                        LogResults = true,
                        LogErrorsAsWarnings = false,
                        ParameterValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "License Key", string.Join(",", StoredProcs.LicenseKeys_GetLicenseKeys()
                                                .Execute()
                                                .Select(k => k.LicenseKey_Text.Remove(5, 27).Insert(5, "..."))) }
                        }
                    }));
            }

            // Other Environments
            for (int i = 1; i < WorkflowSteps.Length; i++)
            {
                int environmentId = WorkflowSteps[i];
                string environmentName = StoredProcs.Environments_GetEnvironment(environmentId).ExecuteDataRow()[TableDefs.Environments.Environment_Name].ToString();

                // Deploy Web
                planId = CreatePlan("Web", environmentId, "Deploy Web", Properties.Resources.BitChecker_DeployWeb);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                    {
                        ArtifactName = "Web",
                        OverriddenTargetDirectory = Path.Combine(this.DeploymentPath, environmentName)
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Configuration.DeployConfigurationFileAction", new
                    {
                        ConfigurationFileId = configurationFileId,
                        InstanceName = environmentName,
                        OverriddenSourceDirectory = Path.Combine(this.DeploymentPath, environmentName)
                    }));

                // Deploy Database
                planId = CreatePlan("Database", environmentId, "Deploy Database", Properties.Resources.BitChecker_DeployDatabase);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Database.ExecuteDatabaseChangeScriptsAction", new
                    {
                        ProviderId = databaseProviderId
                    }));
            }
        }

        private void CreateApproval()
        {
            int? directoryProviderId = InedoLib.Util.Int.ParseN(StoredProcs.Configuration_GetValue("CoreEx", "DirectoryProvider", null).Execute());

            if (directoryProviderId != 1)
                return;

            var admin = StoredProcs.Users_GetUsers().Execute().Users.FirstOrDefault(u => u.User_Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));
            if (admin == null)
                return;

            StoredProcs.Workflows_AddOrRemoveApproval(
                this.WorkflowId,
                this.WorkflowSteps[1],
                null,
                admin.User_Name,
                "Approved by Admin",
                "U",
                Domains.YN.No,
                null,
                "A",
                Domains.YN.No).Execute();
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

        private static void CreateConfigFileInstance(string releaseNumber, int configurationFileId, string environmentName, int environentId)
        {
            StoredProcs.ConfigurationFiles_CreateConfigurationFileInstance(
                configurationFileId,
                environmentName,
                environentId,
                Domains.YN.No,
                null).ExecuteNonQuery();

            string configFileXml;

            using (var configBuffer = new MemoryStream())
            using (var configFileWriter = XmlWriter.Create(configBuffer, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                configFileWriter.WriteStartElement("appSettings");

                configFileWriter.WriteStartElement("add");
                configFileWriter.WriteAttributeString("key", "ContactFormRecipient");
                configFileWriter.WriteAttributeString("value", "contact-form-" + environmentName + "@example.com");
                configFileWriter.WriteEndElement();

                configFileWriter.WriteStartElement("add");
                configFileWriter.WriteAttributeString("key", "SmtpServer");
                configFileWriter.WriteAttributeString("value", environmentName + "-mail-server");
                configFileWriter.WriteEndElement();

                configFileWriter.WriteEndElement();

                configFileWriter.Flush();

                var filesBuffer = new StringBuilder();
                using (var filesWriter = XmlWriter.Create(filesBuffer, new XmlWriterSettings { Encoding = Encoding.Unicode, Indent = false, NewLineChars = "" }))
                {
                    filesWriter.WriteStartElement("ConfigFiles");
                    filesWriter.WriteStartElement("Version");
                    filesWriter.WriteAttributeString("Instance_Name", environmentName);
                    filesWriter.WriteAttributeString("VersionNotes_Text", "Created automatically.");
                    filesWriter.WriteAttributeString("Release_Number", releaseNumber);
                    filesWriter.WriteAttributeString("File_Bytes", Convert.ToBase64String(configBuffer.ToArray()));
                    filesWriter.WriteEndElement();
                    filesWriter.WriteEndElement();
                }

                configFileXml = filesBuffer.ToString();
            }

            StoredProcs.ConfigurationFiles_CreateConfigurationFileVersions(
                configurationFileId,
                configFileXml
            ).ExecuteNonQuery();
        }
        private static int AddAction(int planId, ActionBase action)
        {
            var actionNameProvider = action as IActionNameProvider;

            var proc = StoredProcs.Plans_CreateOrUpdateAction(
                planId,
                null,
                action is AgentBasedActionBase ? (int?)1 : null,
                null,
                action.ToString(),
                Domains.YN.No,
                Util.Persistence.SerializeToPersistedObjectXml(action),
                actionNameProvider != null 
                    ? actionNameProvider.ActionName
                    : Util.Reflection.GetCustomAttribute<ActionPropertiesAttribute>(action.GetType()).Name,
                Domains.YN.Yes,
                0,
                "N",
                null,
                null,
                null
            );

            proc.ExecuteNonQuery();

            return proc.Action_Sequence.Value;
        }
        private static int AddTransferFilesAction(int planId, object properties)
        {
            var action = Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Files.TransferFilesAction",
                properties
            );

            var desc = action.GetActionDescription();
            var shortDesc = desc.ShortDescription != null ? desc.ShortDescription.ToString() : null;
            var longDesc = desc.LongDescription != null ? desc.LongDescription.ToString() : null;

            var proc = StoredProcs.Plans_CreateOrUpdateAction(
                ActionGroup_Id: planId,
                Server_Id: 1,
                Short_Description: shortDesc,
                Long_Description: longDesc,
                ResumeNextOnFailure_Indicator: Domains.YN.No,
                Action_Configuration: Util.Persistence.SerializeToPersistedObjectXml(action),
                Active_Indicator: Domains.YN.Yes,
                Retry_Count: 0,
                LogFailureAsWarning_Indicator: Domains.YN.No,
                Target_Server_Id: 1
            );

            proc.ExecuteNonQuery();

            return proc.Action_Sequence.Value;
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

            var webUserContextType = Type.GetType("Inedo.BuildMaster.Web.Security.WebUserContext,BuildMaster", true);
            var canPerformTask = webUserContextType
                .GetMethods()
                .First(m => m.Name == "CanPerformTask" && m.GetParameters().Length == 5);
            if (!(bool)canPerformTask.Invoke(null, new object[] { 6 /*Builds_CreateBuild*/, null, applicationId, null, null }))
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
                Build_Number: null
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

        private int CreatePlan(string deployableName, int environmentId, string planName, string planDesc)
        {
            var proc = StoredProcs.Plans_CreateDeploymentPlanActionGroup(
                Environment_Id: environmentId,
                Application_Id: this.ApplicationId,
                Deployable_Name: deployableName,
                Active_Indicator: Domains.YN.Yes,
                ActionGroup_Name: planName,
                ActionGroup_Description: planDesc
            );
            proc.ExecuteNonQuery();
            return proc.ActionGroup_Id.Value;
        }
    }
}
