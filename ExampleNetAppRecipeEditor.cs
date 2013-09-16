using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Configuration;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.DotNetRecipes
{
    /// <summary>
    /// Custom editor for the .NET example application recipe.
    /// </summary>
    public sealed class ExampleNetAppRecipeEditor : RecipeEditorBase
    {
        private SourceControlFileFolderPicker txtDeploymentPath;
        private CheckBox chkUseClickOnce;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleNetAppRecipeEditor"/> class.
        /// </summary>
        public ExampleNetAppRecipeEditor()
        {
        }

        public override RecipeBase CreateFromForm()
        {
            return new ExampleNetAppRecipe
            {
                DeploymentPath = txtDeploymentPath.Text,
                UseClickOnce = chkUseClickOnce.Checked,
            };
        }

        protected override void CreateChildControls()
        {
            this.txtDeploymentPath = new SourceControlFileFolderPicker
            {
                Text = Path.Combine(
                    Directory.GetParent(CoreConfig.BaseWorkingDirectory).FullName, 
                    Path.Combine("Demos", "MiniCalc")),
                Width = 300,
                Required = true,
                ServerId = 1
            };

            this.chkUseClickOnce = new CheckBox
            {
                Text = "Publish using ClickOnce deployment (Windows/.NET SDK is required)"
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Deployment Base Path",
                    "The root path where items will be deployed. Under this directory, a directory will be created for each environment in the workflow.",
                    false,
                    new StandardFormField("Path:", txtDeploymentPath)),
                new FormFieldGroup(
                    "ClickOnce",
                    "This example may be published using ClickOnce deployment.",
                    true,
                    new StandardFormField(string.Empty, chkUseClickOnce)
                )
            );
        }
    }
}
