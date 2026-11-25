namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Spec
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class OrderingMetamethodTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void OrderingChecksOperandTypesBeforeDispatch(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<OrderingMetamethodTUnitTests>();
            script.DoFile(fixture.GetFixturePath());
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void OrderingSelectsMetamethodByProfile(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper fixture =
                LuaFixtureHelper.ForCallingTest<OrderingMetamethodTUnitTests>();
            script.DoFile(fixture.GetFixturePath());
        }
    }
}
