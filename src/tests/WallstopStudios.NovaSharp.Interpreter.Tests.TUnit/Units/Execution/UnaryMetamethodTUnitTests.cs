namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class UnaryMetamethodTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void LengthUsesVersionedUnaryMetamethodRules(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<UnaryMetamethodTUnitTests>().RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void UnaryMinusPassesItsOperandTwice(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<UnaryMetamethodTUnitTests>().RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void DirectUnaryMetamethodYieldFollowsLuaVersion(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<UnaryMetamethodTUnitTests>().RunFixture(script);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public void BitwiseNotPassesItsOperandTwice(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper.ForCallingTest<UnaryMetamethodTUnitTests>().RunFixture(script);
        }
    }
}
