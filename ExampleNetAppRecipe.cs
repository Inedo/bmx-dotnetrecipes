using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.DotNetRecipes.Providers;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    [RecipeProperties(
        "Calculator",
        "Creates a new application that pulls code from a sample repository, compiles the project, and then optionally publishes the application using ClickOnce.",
        RecipeScopes.Example)]
    [CustomEditor(typeof(ExampleNetAppRecipeEditor))]
    [WorkflowCreatingRequirements(MinimumEnvironmentCount = 2)]
    public sealed class ExampleNetAppRecipe : RecipeBase, IApplicationCreatingRecipe, IWorkflowCreatingRecipe, IScmCreatingRecipe
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleNetAppRecipe"/> class.
        /// </summary>
        public ExampleNetAppRecipe()
        {
        }

        public string DeploymentPath { get; set; }
        public bool UseClickOnce { get; set; }
        public string ClickOnceUrl { get; set; }

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

        public override void Execute()
        {
            var workflowSteps = StoredProcs
                .Workflows_GetWorkflow(WorkflowId)
                .ExecuteDataSet(TableNames.Workflows_Extended, TableNames.WorkflowSteps_Extended)
                .Tables[TableNames.WorkflowSteps_Extended];

            // Create Deployable
            int deployableId;
            {
                var proc = StoredProcs
                    .Applications_CreateOrUpdateDeployable(
                        null,
                        ApplicationId,
                        "App",
                        Domains.DeployableTypes.Other);
                proc.ExecuteNonQuery();
                deployableId = proc.Deployable_Id.Value;
            }

            // Create Release
            string releaseNumber = "1.0";
            StoredProcs.Releases_CreateOrUpdateRelease(
                ApplicationId,
                releaseNumber,
                WorkflowId,
                null,
                null,
                null,
                "<ReleaseDeployables><ReleaseDeployable Deployable_Id=\"" + deployableId.ToString() + "\" InclusionType_Code=\"I\" /></ReleaseDeployables>")
                .ExecuteNonQuery();

            int planId;
            // First Environment
            {
                int environmentId = WorkflowSteps[0];
                string environmentName = StoredProcs.Environments_GetEnvironment(environmentId).ExecuteDataRow()[TableDefs.Environments.Environment_Name].ToString();

                // Source
                planId = CreatePlan(environmentId, "Source");
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLatestAction", new
                    {
                        OverriddenTargetDirectory = @"~\Source",
                        SourcePath = "/TRUNK/MiniCalc/",
                        ProviderId = this.ScmProviderId
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Files.ReplaceFileAction", new
                    {
                        FileNameMasks = new[]{ "AssemblyInfo.cs" },
                        OverriddenSourceDirectory = @"~\Source\Properties",
                        SearchText = "Version(\"1.0.0.0\")",
                        ReplaceText = "Version(\"%RELNO%.0.%BLDNO%\")",
                        UseRegex = false
                    }));
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                    {
                        OverriddenSourceDirectory = @"~\Source",
                        ArtifactName = "App-SourceFiles"
                    }));

                // Build
                planId = CreatePlan(environmentId, "Build");
                AddAction(planId,
                    (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.WindowsSdk.MSBuild.BuildMSBuildProjectAction,WindowsSdk",
                    new
                    {
                        OverriddenSourceDirectory = @"~\Source",
                        OverriddenTargetDirectory = @"~\BuildOutput",
                        IsWebProject = false,
                        ProjectBuildConfiguration = "Debug",
                        ProjectPath = "MiniCalc.csproj"
                    }));
                if (UseClickOnce)
                {
                    AddAction(planId,
                        (ActionBase)Util.Recipes.Munging.MungeInstance("Inedo.BuildMasterExtensions.WindowsSdk.DotNet.ClickOnceAction,WindowsSdk",
                        new
                        {
                            OverriddenSourceDirectory = @"~\BuildOutput",
                            ApplicationName = "MiniCalc",
                            Version = "%RELNO%.0.%BLDNO%",
                            ProviderUrl = Path.Combine(DeploymentPath, environmentName),
                            CertificatePath = @"..\Source\MiniCalc.pfx",
                            CertificatePassword = "password"
                        }));
                }
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                    {
                        OverriddenSourceDirectory = @"~\BuildOutput",
                        ArtifactName = "App"
                    }));

                // Deploy
                planId = CreatePlan(environmentId, "Deploy");
                AddTransferFilesAction(
                    planId,
                    new
                    {
                        IncludeFileMasks = new[] { "*" },
                        DeleteTarget = true,
                        SourceDirectory = @"~\BuildOutput",
                        TargetDirectory = Path.Combine(DeploymentPath, environmentName),
                    }
                );
            }

            // Other Environments
            for (int i = 1; i < WorkflowSteps.Length; i++)
            {
                int environmentId = WorkflowSteps[i];
                string environmentName = StoredProcs.Environments_GetEnvironment(environmentId).ExecuteDataRow()[TableDefs.Environments.Environment_Name].ToString();

                // Deploy
                planId = CreatePlan(environmentId, "Deploy");
                AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction",new
                    {
                        ArtifactName = "App",
                        OverriddenTargetDirectory = Path.Combine(DeploymentPath, environmentName)
                    }));
            }
        }

        public SourceControlProviderBase InstantiateSourceControlProvider()
        {
            return new ExampleSourceControlProvider();
        }

        private int CreatePlan(int environmentId, string planName)
        {
            var proc = StoredProcs.Plans_CreateDeploymentPlanActionGroup(
                Environment_Id: environmentId,
                Application_Id: this.ApplicationId,
                Deployable_Name: "App",
                Active_Indicator: Domains.YN.Yes,
                ActionGroup_Name: planName
            );
            proc.ExecuteNonQuery();
            return proc.ActionGroup_Id.Value;
        }
        private int AddAction(int planId, ActionBase action)
        {
            var desc = action.GetActionDescription();
            var shortDesc = desc.ShortDescription != null ? desc.ShortDescription.ToString() : null;
            var longDesc = desc.LongDescription != null ? desc.LongDescription.ToString() : null;

            var proc = StoredProcs.Plans_CreateOrUpdateAction(
                ActionGroup_Id: planId,
                Server_Id: action is RemoteActionBase ? (int?)1 : null,
                Short_Description: shortDesc,
                ResumeNextOnFailure_Indicator: Domains.YN.No,
                Action_Configuration: Util.Persistence.SerializeToPersistedObjectXml(action),
                Long_Description: longDesc,
                Active_Indicator: Domains.YN.Yes,
                Retry_Count: 0,
                LogFailureAsWarning_Indicator: Domains.YN.No
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
    }
}
