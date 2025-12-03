namespace NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class TestMoreTUnitTests
    {
        private static Task RunTapAsync(
            string relativePath,
            LuaCompatibilityVersion? compatibilityVersion = null
        )
        {
            using UserDataIsolationScope scope = UserDataIsolationScope.Begin();
            TapRunnerTUnit.Run(relativePath, compatibilityVersion);

            return Task.CompletedTask;
        }

        [Test]
        [MethodDataSource(nameof(GetTestMoreSuiteData))]
        public Task RunTestMoreSuite(string relativePath, LuaCompatibilityVersion? compatibility)
        {
            return RunTapAsync(relativePath, compatibility);
        }

        public static IEnumerable<object[]> GetTestMoreSuiteData()
        {
            foreach (TapSuiteDefinition suite in TapSuiteCatalog.GetTestMoreSuites())
            {
                yield return new object[] { suite.RelativePath, suite.CompatibilityVersion };
            }
        }
    }
}
