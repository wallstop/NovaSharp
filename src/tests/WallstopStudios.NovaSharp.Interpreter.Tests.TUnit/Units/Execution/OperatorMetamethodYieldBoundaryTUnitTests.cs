namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution
{
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.LuaFixtures;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class OperatorMetamethodYieldBoundaryTUnitTests
    {
        [Test]
        [AllLuaVersions]
        public void NestedOperatorYieldFollowsLuaVersion(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodYieldBoundaryTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void IndexHooksUseVersionedYieldBoundary(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodYieldBoundaryTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public void ProtectedOperatorYieldLeavesCoroutineUsable(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodYieldBoundaryTUnitTests>()
                .RunFixture(script);
        }

        [Test]
        [AllLuaVersions]
        public async Task HostTailRequestKeepsOperatorBoundaryAcrossAutoYield(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            bool enteredHook = false;
            LuaValue hook = LuaValue.Nil;
            script.Globals.Set(
                "makeHostTailRequest",
                LuaValue.NewCallback((_, _) => LuaValue.NewTailCallReq(hook))
            );
            script.Globals.Set(
                "hostYield",
                LuaValue.NewCallback((_, _) => LuaValue.NewYieldReq(LuaValue.NewString("host")))
            );
            script.Globals.Set(
                "markHookEntered",
                LuaValue.NewCallback(
                    (_, _) =>
                    {
                        enteredHook = true;
                        return LuaValue.Void;
                    }
                )
            );

            LuaValue[] fixtureResults = LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodYieldBoundaryTUnitTests>()
                .RunFixture(script)
                .Tuple;
            hook = fixtureResults[0];
            Coroutine coroutine = script.CreateCoroutineValue(fixtureResults[1]).Coroutine;
            coroutine.AutoYieldCounter = 1;
            LuaValue result = LuaValue.Nil;
            for (int i = 0; i < 128 && !enteredHook; i++)
            {
                result = coroutine.Resume();
                await Assert
                    .That(result.Type)
                    .IsEqualTo(DataType.YieldRequest)
                    .ConfigureAwait(false);
                await Assert.That(result.YieldRequest.Forced).IsTrue().ConfigureAwait(false);
            }
            await Assert.That(enteredHook).IsTrue().ConfigureAwait(false);
            await Assert
                .That(coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended)
                .ConfigureAwait(false);

            coroutine.AutoYieldCounter = 0;
            result = coroutine.Resume();
            if (version != LuaCompatibilityVersion.Lua51)
            {
                await Assert.That(result.String).IsEqualTo("host").ConfigureAwait(false);
                result = coroutine.Resume(LuaValue.FromNumber(42));
            }

            await Assert.That(result.String).IsEqualTo("recovered").ConfigureAwait(false);
            result = coroutine.Resume(LuaValue.True, LuaValue.False);
            await Assert.That(result.Tuple.Length).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0]).IsEqualTo(LuaValue.True).ConfigureAwait(false);
            await Assert.That(result.Tuple[1]).IsEqualTo(LuaValue.False).ConfigureAwait(false);
            await Assert.That(coroutine.State).IsEqualTo(CoroutineState.Dead).ConfigureAwait(false);
            await Assert
                .That(coroutine.GetProcessorForTests().GetExecutionStackForTests().Count)
                .IsEqualTo(0)
                .ConfigureAwait(false);
            await Assert
                .That(coroutine.GetProcessorForTests().GetValueStackForTests().Count)
                .IsEqualTo(0)
                .ConfigureAwait(false);
        }

        [Test]
        [AllLuaVersions]
        public async Task OperatorHookUsesTheCoroutineInstructionBudget(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    CompatibilityVersion = version,
                    Sandbox = new SandboxOptions { MaxInstructions = 200 },
                }
            );
            bool enteredHook = false;
            script.Globals.Set(
                "markHookEntered",
                LuaValue.NewCallback(
                    (_, _) =>
                    {
                        enteredHook = true;
                        return LuaValue.Void;
                    }
                )
            );
            string source = LuaFixtureHelper
                .ForCallingTest<OperatorMetamethodYieldBoundaryTUnitTests>()
                .LoadFixtureContent();
            LuaValue body = script.DoString(source);
            Coroutine coroutine = script.CreateCoroutineValue(body).Coroutine;
            SandboxViolationException exception = await Assert
                .That(() => coroutine.Resume())
                .Throws<SandboxViolationException>()
                .ConfigureAwait(false);

            await Assert.That(enteredHook).IsTrue().ConfigureAwait(false);
            await Assert
                .That(exception.ViolationType)
                .IsEqualTo(SandboxViolationType.InstructionLimitExceeded)
                .ConfigureAwait(false);
            await Assert.That(coroutine.State).IsEqualTo(CoroutineState.Dead).ConfigureAwait(false);
        }
    }
}
