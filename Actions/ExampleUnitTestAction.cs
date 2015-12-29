using System;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Actions.Testing;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Actions
{
    [ActionProperties(
        "Execute Unit Tests",
        "Runs unit tests on a project.")]
    [Tag("Testing")]
    [CustomEditor(typeof(ExampleUnitTestActionEditor))]
    internal sealed class ExampleUnitTestAction : UnitTestActionBase
    {
        // This is just here to ensure that an error occurs if this action gets copied or shared
        // into another application.
        [Persistent]
        public int BitCheckerApplicationId { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription("Run Unit Tests on ", new Hilite("BitChecker"))
            );
        }

        protected override void RunTests()
        {
            if (this.BitCheckerApplicationId != this.Context.ApplicationId)
            {
                this.LogError("This action is for demonstration purposes and may only be used in the BitChecker example application. " +
                         "To add unit testing to your deployment plan, add the unit test action for your project's unit test framework " +
                         "(ex. NUnit, JUnit, Gallio)");
                return;
            }

            this.GroupName = "UnitTests";

            this.RecordResult("CheckBits_InvalidArgs", true, "success", DateTime.UtcNow, DateTime.UtcNow);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_EvenBits", true, "success", DateTime.UtcNow, DateTime.UtcNow);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_OddBits", true, "success", DateTime.UtcNow, DateTime.UtcNow);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_1000Bits", true, "success", DateTime.UtcNow, DateTime.UtcNow);
            Thread.Sleep(500);
        }
    }
}
