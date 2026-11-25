namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class OrderingDiagnosticTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void FailedOperatorsNameBothOperands(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<OrderingDiagnosticTUnitTests>().RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void DefaultSortNamesBothOperands(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<OrderingDiagnosticTUnitTests>().RunFixture(script);
        }
    }
}
