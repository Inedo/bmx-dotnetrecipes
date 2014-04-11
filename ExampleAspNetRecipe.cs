using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
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

            // Create Previous Release
            StoredProcs.Releases_CreateOrUpdateRelease(
                this.ApplicationId,
                "1.0",
                this.WorkflowId,
                null,
                null,
                null,
                "<ReleaseDeployables><ReleaseDeployable Deployable_Id=\"" + deployableId.ToString() + "\" InclusionType_Code=\"I\" />" +
                "<ReleaseDeployable Deployable_Id=\"" + databaseDeployableId.ToString() + "\" InclusionType_Code=\"I\" /></ReleaseDeployables>")
                .ExecuteNonQuery();

            // Create build in old release

            StoredProcs.Builds_CreateBuild(this.ApplicationId, "1.0", "Y", "Y", DateTime.UtcNow, null, null, null, null, null).Execute();

            // auto-promote build until old release is deployed
            for (int i = 0; i < workflowSteps.Rows.Count - 1; i++)
            {
                StoredProcs.Builds_PromoteBuild(
                    this.ApplicationId,
                    "1.0",
                    "1",
                    null,
                    DateTime.UtcNow,
                    "Y",
                    "N",
                    (int)workflowSteps.Rows[i][TableDefs.WorkflowSteps_Extended.Next_Environment_Id],
                    null, null).ExecuteNonQuery();
            }

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
                    "Y");

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
                planId = CreatePlan(deployableId, environmentId, "Source", Properties.Resources.BitChecker_GetSource);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "%RELNO%.%BLDNO%",
                        ProviderId = this.ScmProviderId
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "%RELNO%.%BLDNO%",
                        ProviderId = this.ScmProviderId
                    }));
                AddAction(planId,
                    (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.WindowsSdk.DotNet.WriteAssemblyInfoVersionsAction,WindowsSdk",
                    new
                    {
                        FileMasks = new[] { "*\\AssemblyInfo.cs" },
                        Recursive = true,
                        Version = "%RELNO%.%BLDNO%"
                    }));

                // Compare Source
                planId = CreatePlan(deployableId, environmentId, "Compare Source", Properties.Resources.BitChecker_CompareSource);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "%PREVRELNO%.%PREVBLDNO%",
                        ProviderId = this.ScmProviderId,
                        OverriddenTargetDirectory = "PrevSrc"
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                    {
                        SourcePath = "/TRUNK/BitChecker/",
                        UserDefinedLabel = "%RELNO%.%BLDNO%",
                        ProviderId = this.ScmProviderId,
                        OverriddenTargetDirectory = "CurrentSrc"
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Reporting.CompareDirectoriesReportingAction", new
                    {
                        IncludeUnchanged = false,
                        Path1 = "PrevSrc",
                        Path2 = "CurrentSrc",
                        OutputName = "Changes Between %PREVRELNO%.%PREVBLDNO% and %RELNO%.%BLDNO%"
                    }));

                // Build
                planId = CreatePlan(deployableId, environmentId, "Build", Properties.Resources.BitChecker_Build);
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

                planId = CreatePlan(deployableId, environmentId, "Unit Tests", Properties.Resources.BitChecker_UnitTests);
                AddAction(planId, new ExampleUnitTestAction { BitCheckerApplicationId = this.ApplicationId });

                // Deploy Web
                planId = CreatePlan(deployableId, environmentId, "Deploy Web", Properties.Resources.BitChecker_DeployWeb);

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
                planId = CreatePlan(databaseDeployableId, environmentId, "Deploy Database", Properties.Resources.BitChecker_DeployDatabase);
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Database.ExecuteDatabaseChangeScriptsAction", new
                    {
                        ProviderId = databaseProviderId
                    }));

                // Say Hello to Inedo
                planId = CreatePlan(deployableId, environmentId, "Say Hello to Inedo", Properties.Resources.BitChecker_SayHello);

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
                planId = CreatePlan(deployableId, environmentId, "Deploy Web", Properties.Resources.BitChecker_DeployWeb);
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
                planId = CreatePlan(databaseDeployableId, environmentId, "Deploy Database", Properties.Resources.BitChecker_DeployDatabase);
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
                "N",
                null,
                "A",
                "N").Execute();
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
                    dashboardText = reader.ReadToEnd();
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

            var proc = StoredProcs.Plans_CreateOrUpdateAction(
                Plan_Id: planId,
                Server_Id: 1,
                Action_Description: action.ToString(),
                ResumeNextOnFailure_Indicator: Domains.YN.No,
                Action_Configuration: Util.Persistence.SerializeToPersistedObjectXml(action),
                ActionType_Name: Util.Reflection.GetCustomAttribute<ActionPropertiesAttribute>(action.GetType()).Name,
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

        private int CreatePlan(int deployableId, int environmentId, string planName, string planDesc)
        {
            var proc = StoredProcs.Plans_CreatePlanActionGroup(
                Deployable_Id: deployableId,
                Environment_Id: environmentId,
                Application_Id: this.ApplicationId,
                Active_Indicator: Domains.YN.Yes,
                ActionGroup_Name: planName,
                ActionGroup_Description: planDesc
            );
            proc.ExecuteNonQuery();
            return proc.ActionGroup_Id.Value;
        }
    }
}
