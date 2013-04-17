using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Actions
{
    /// <summary>
    /// Custom editor for the Example Unit Test Action.
    /// </summary>
    internal sealed class ExampleUnitTestActionEditor : ActionEditorBase
    {
        private int bitCheckerId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleUnitTestActionEditor"/> class.
        /// </summary>
        public ExampleUnitTestActionEditor()
        {
        }

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
