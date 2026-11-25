namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class StringOrderingTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void BuiltInStringOrderingMatchesCByteOrder(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<StringOrderingTUnitTests>().RunFixture(script);
        }
    }
}
