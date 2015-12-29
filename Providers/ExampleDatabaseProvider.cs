using System;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.Database;

namespace Inedo.BuildMasterExtensions.DotNetRecipes.Providers
{
    [ProviderProperties(
        "Example",
        "Sample database provider for use only with example applications.")]
    internal sealed class ExampleDatabaseProvider : DatabaseProviderBase, IChangeScriptProvider
    {
        private const long NumericReleaseNumber10 = 2821109907456;

        public override void ExecuteQueries(string[] queries)
        {
        }
        public override void ExecuteQuery(string query)
        {
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
        }

        public ExecutionResult ExecuteChangeScript(long numericReleaseNumber, int scriptId, string scriptName, string scriptText)
        {
            return new ExecutionResult(ExecutionResult.Results.Skipped, "Script already ran.");
        }
        public ChangeScript[] GetChangeHistory()
        {
            return new[]
            {
                new ExampleChangeScript(1, "10.Create Database Schema", new DateTime(2010, 1, 20)),
                new ExampleChangeScript(2, "20.Add Table [BitSources]", new DateTime(2010, 1, 22)),
                new ExampleChangeScript(3, "30.Add [User_Name] Column to [Bits] Table", new DateTime(2010, 2, 10))
            };
        }
        public long GetSchemaVersion()
        {
            return NumericReleaseNumber10;
        }
        public void InitializeDatabase()
        {
        }
        public bool IsDatabaseInitialized()
        {
            return true;
        }

        public override string ToString()
        {
            return "Sample database provider for use only with example applications.";
        }

        [Serializable]
        private sealed class ExampleChangeScript : ChangeScript
        {
            public ExampleChangeScript(int scriptId, string scriptName, DateTime executionDate)
                : base(NumericReleaseNumber10, scriptId, scriptName, executionDate, true)
            {
            }
        }
    }
}
