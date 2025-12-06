namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Tests;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class TestMoreTUnitTests
    {
        private static readonly SemaphoreSlim TapSuiteGate = new(1, 1);

        private static async Task RunTapAsync(
            string relativePath,
            LuaCompatibilityVersion? compatibilityVersion = null
        )
        {
            SemaphoreSlimLease gateLease = await SemaphoreSlimScope
                .WaitAsync(TapSuiteGate)
                .ConfigureAwait(false);

            using (gateLease)
            using (UserDataIsolationScope scope = UserDataIsolationScope.Begin())
            {
                TapRunnerTUnit.Run(relativePath, compatibilityVersion);
            }
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
