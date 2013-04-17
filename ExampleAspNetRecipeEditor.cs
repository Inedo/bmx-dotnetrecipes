using System.IO;
using Inedo.BuildMaster.Configuration;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    public sealed class ExampleAspNetRecipeEditor : RecipeEditorBase
    {
        SourceControlFileFolderPicker txtDeploymentPath;

        public ExampleAspNetRecipeEditor()
        {
        }

        protected override void CreateChildControls()
        {
            // txtDeploymentPath
            txtDeploymentPath = new SourceControlFileFolderPicker
            {
                Text = Path.Combine(
                    Directory.GetParent(CoreConfig.BaseWorkingDirectory).FullName,
                    Path.Combine("Demos", "BitChecker")),
                Required = true,
                ServerId = 1
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Deployment Base Path",
                    "The root path where items will be deployed. Under this directory, a directory will be created for each environment in the workflow.",
                    true,
                    new StandardFormField("Path:", txtDeploymentPath))
                );
        }

        public override RecipeBase CreateFromForm()
        {
            return new ExampleAspNetRecipe
            {
                DeploymentPath = txtDeploymentPath.Text
            };
        }
    }
}
