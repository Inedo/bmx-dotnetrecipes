using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Configuration;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;

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
            ValidateBeforeSave += ExampleAspNetRecipeEditor_ValidateBeforeSave;
        }

        protected override void CreateChildControls()
        {
            // txtDeploymentPath
            txtDeploymentPath = new SourceControlFileFolderPicker
            {
                Text = Path.Combine(
                    Directory.GetParent(CoreConfig.BaseWorkingDirectory).FullName, 
                    Path.Combine("Demos", "MiniCalc")),
                Width = 300,
                Required = true,
                ServerId = 1
            };

            chkUseClickOnce = new CheckBox
            {
                Text = "Publish using ClickOnce deployment (Windows/.NET SDK is required)"
            };

            CUtil.Add(this,
                new FormFieldGroup(
                    "Deployment Base Path",
                    "The root path where items will be deployed. Under this directory, a directory will be created for each environment in the workflow.",
                    false,
                    new StandardFormField("Path:", txtDeploymentPath)),
                new FormFieldGroup(
                    "ClickOnce",
                    "This example may be published using ClickOnce deployment.",
                    true,
                    new StandardFormField("", chkUseClickOnce)
                ));
        }

        public override RecipeBase CreateFromForm()
        {
            return new ExampleNetAppRecipe
            {
                DeploymentPath = txtDeploymentPath.Text,
                UseClickOnce = chkUseClickOnce.Checked,
            };
        }

        private void ExampleAspNetRecipeEditor_ValidateBeforeSave(object sender, ValidationEventArgs<RecipeBase> e)
        {
            var cfg = GetConfigurer();
            var helper = Activator.CreateInstance(Type.GetType("Inedo.BuildMasterExtensions.DotNet2.DotNet2Helper,DotNet2", true));
            var isMageAvailable = (bool)helper.GetType().GetMethod("IsMageAvailable").Invoke(helper, new object[] { (string)MungeUtil.GetPropertyValue(cfg, "SdkPath")});
            
            if (chkUseClickOnce.Checked && !isMageAvailable)
            {
                e.ValidLevel = ValidationLevels.Warning;
                e.Message = "The Windows or .NET Framework SDK does not appear to be installed. ClickOnce deployment may not be available.";
                return;
            }

            if (Directory.Exists(txtDeploymentPath.Text)) return;
            e.ValidLevel = ValidationLevels.Warning;
            e.Message = "The specified deployment path (" + txtDeploymentPath.Text + ") does not exist. It will be created when the recipe is executed.";
        }

        private ExtensionConfigurerBase GetConfigurer()
        {
            var cfg = Type.GetType("Inedo.BuildMasterExtensions.DotNet2.DotNetConfigurer,DotNet2", true);

            var dr = StoredProcs.ExtensionConfiguration_GetConfiguration(cfg.FullName + "," + cfg.Assembly.GetName().Name,
                    null, // TODO Profile_Name
                    Domains.YN.No).ExecuteDataRow();

            if (dr == null)
                return (ExtensionConfigurerBase)Activator.CreateInstance(cfg);
            else
                return Util.Persistence.DeSerializeFromPersistedObjectXml<ExtensionConfigurerBase>(dr[TableDefs.ExtensionConfigurations.Extension_Configuration].ToString());
        }
    }
}
