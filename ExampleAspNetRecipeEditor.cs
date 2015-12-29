using Inedo.BuildMaster.Configuration;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    internal sealed class ExampleAspNetRecipeEditor : RecipeEditorBase
    {
        private SourceControlFileFolderPicker txtDeploymentPath;

        protected override void CreateChildControls()
        {
            this.txtDeploymentPath = new SourceControlFileFolderPicker
            {
                Text = PathEx.Combine(PathEx.GetDirectoryName(CoreConfig.BaseWorkingDirectory), "Demos", "BitChecker"),
                Required = true,
                ServerId = 1
            };

            this.Controls.Add(
                new SlimFormField("Deployment path:", this.txtDeploymentPath)
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
