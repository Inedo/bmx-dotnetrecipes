using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Actions
{
    internal sealed class ExampleUnitTestActionEditor : ActionEditorBase
    {
        private int bitCheckerId;

        public override void BindToForm(ActionBase extension)
        {
            this.bitCheckerId = ((ExampleUnitTestAction)extension).BitCheckerApplicationId;
        }
        public override ActionBase CreateFromForm()
        {
            return new ExampleUnitTestAction
            {
                BitCheckerApplicationId = this.bitCheckerId
            };
        }
    }
}
