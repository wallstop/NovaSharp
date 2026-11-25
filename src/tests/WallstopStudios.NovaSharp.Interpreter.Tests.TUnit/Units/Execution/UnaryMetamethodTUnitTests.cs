namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
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
        [AllLuaVersions]
        public async Task CallbackCannotForwardChildAutoYield(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue childValue = script.CreateCoroutineValue(
                script.LoadString("local n = 0; for i = 1, 20 do n = n + 1 end; return n + 22")
            );
            Coroutine child = childValue.Coroutine;
            child.AutoYieldCounter = 1;
            LuaValue forward = LuaValue.NewCallback((_, _) => child.Resume());
            script.Globals.Set("autoYieldChild", childValue);
            script.Globals.Set("forwardForcedYield", forward);
            script.Globals.Set(
                "tailForwardForcedYield",
                LuaValue.NewCallback((_, _) => LuaValue.NewTailCallReq(forward))
            );
            LuaValue body = LuaFixtureHelper
                .ForCallingTest<UnaryMetamethodTUnitTests>()
                .RunFixture(script);
            Coroutine parent = script.CreateCoroutineValue(body).Coroutine;

            LuaValue first = parent.Resume();
            await Assert.That(first.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(first.String).IsEqualTo("ready").ConfigureAwait(false);
            LuaValue result = parent.Resume(LuaValue.True, LuaValue.False);
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0]).IsEqualTo(LuaValue.True).ConfigureAwait(false);
            await Assert.That(result.Tuple[1]).IsEqualTo(LuaValue.False).ConfigureAwait(false);
            await Assert.That(parent.State).IsEqualTo(CoroutineState.Dead).ConfigureAwait(false);
            await Assert
                .That(child.State)
                .IsEqualTo(CoroutineState.ForceSuspended)
                .ConfigureAwait(false);
            child.AutoYieldCounter = 0;
            await Assert.That(child.Resume().Number).IsEqualTo(42d).ConfigureAwait(false);
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
