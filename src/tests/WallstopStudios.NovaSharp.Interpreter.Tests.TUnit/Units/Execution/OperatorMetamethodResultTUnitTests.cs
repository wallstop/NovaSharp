namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using global::NovaSharp;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class OperatorMetamethodResultTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void OperatorHooksProduceExactlyOneValue(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodResultTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public void OperatorHooksAdjustResultsAfterSuspension(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodResultTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void HostOperatorHooksProduceExactlyOneValue(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue many = LuaValue.NewCallback(
                (_, _) =>
                    LuaValue.NewTuple(
                        LuaValue.FromNumber(7),
                        LuaValue.FromNumber(8),
                        LuaValue.FromNumber(9)
                    )
            );
            LuaValue luaTailTarget = script.LoadString("return 7, 8, 9");
            script.Globals.Set("hostMany", many);
            script.Globals.Set("hostNone", LuaValue.NewCallback((_, _) => LuaValue.Void));
            script.Globals.Set(
                "hostTail",
                LuaValue.NewCallback((_, _) => LuaValue.NewTailCallReq(luaTailTarget))
            );
            script.Globals.Set(
                "hostNativeTail",
                LuaValue.NewCallback((_, _) => LuaValue.NewTailCallReq(many))
            );

            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodResultTUnitTests>()
                .RunFixture(script);
        }
    }
}
