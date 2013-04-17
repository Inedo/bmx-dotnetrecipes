using System;
using System.Threading;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Actions.Testing;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Actions
{
    /// <summary>
    /// Example unit test action for BitChecker.
    /// </summary>
    [ActionProperties(
        "Execute Unit Tests",
        "Runs unit tests on a project.",
        "Testing")]
    [CustomEditor(typeof(ExampleUnitTestActionEditor))]
    internal sealed class ExampleUnitTestAction : UnitTestActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleUnitTestAction"/> class.
        /// </summary>
        public ExampleUnitTestAction()
        {
        }

        /// <summary>
        /// Gets or sets the Id of the associated BitChecker example application.
        /// </summary>
        /// <remarks>
        /// This is just here to ensure that an error occurs if this action gets copied or shared
        /// into another application.
        /// </remarks>
        [Persistent]
        public int BitCheckerApplicationId { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Run Unit Tests on BitChecker.";
        }

        /// <summary>
        /// Runs a unit test against a single DLL, Project File, or test configuration file
        /// After test is run, use RecordResult to save the test result to the database.
        /// </summary>
        protected override void RunTests()
        {
            if (this.BitCheckerApplicationId != this.Context.ApplicationId)
            {
                LogError("This action is for demonstration purposes and may only be used in the BitChecker example application. " +
                         "To add unit testing to your deployment plan, add the unit test action for your project's unit test framework " +
                         "(ex. NUnit, JUnit, Gallio)");
                return;
            }

            this.GroupName = "UnitTests";

            this.RecordResult("CheckBits_InvalidArgs", true, "success", DateTime.Now, DateTime.Now);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_EvenBits", true, "success", DateTime.Now, DateTime.Now);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_OddBits", true, "success", DateTime.Now, DateTime.Now);
            Thread.Sleep(500);
            this.RecordResult("CheckBits_1000Bits", true, "success", DateTime.Now, DateTime.Now);
            Thread.Sleep(500);
        }
    }
}
