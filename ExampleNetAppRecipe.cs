﻿using System;
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
                planId = CreatePlan(deployableId, environmentId, "Source");
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
                planId = CreatePlan(deployableId, environmentId, "Build");
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
                planId = CreatePlan(deployableId, environmentId, "Deploy");
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
                planId = CreatePlan(deployableId, environmentId, "Deploy");
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

        private int CreatePlan(int deployableId, int environmentId, string planName)
        {
            var proc = StoredProcs
                .Plans_CreatePlanActionGroup(
                    null,
                    null,
                    null,
                    deployableId,
                    environmentId,
                    this.ApplicationId,
                    null,
                    null,
                    null,
                    Domains.YN.Yes,
                    planName,
                    null,
                    null,
                    null);
            proc.ExecuteNonQuery();
            return proc.ActionGroup_Id.Value;
        }
        private int AddAction(int planId, ActionBase action)
        {
            var proc = StoredProcs
                .Plans_CreateOrUpdateAction(
                    planId, null,
                    action is RemoteActionBase ? (int?)1 : null,
                    null,
                    action.ToString(),
                    Domains.YN.No,
                    Util.Persistence.SerializeToPersistedObjectXml(action),
                    Util.Reflection.GetCustomAttribute<ActionPropertiesAttribute>(action.GetType()).Name,
                    Domains.YN.Yes,
                    0,
                    "N",
                    null,
                    null,
                    null);
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
                planId,
                null,
                1,
                null,
                action.ToString(),
                Domains.YN.No,
                Util.Persistence.SerializeToPersistedObjectXml(action),
                Util.Reflection.GetCustomAttribute<ActionPropertiesAttribute>(action.GetType()).Name,
                Domains.YN.Yes,
                0,
                "N",
                1,
                null,
                null
            );

            proc.ExecuteNonQuery();

            return proc.Action_Sequence.Value;
        }
    }
}
