namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineModuleTests
    {
        [Test]
        public void RunningFromMainReturnsMainCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);
            Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));

            DynValue coroutineValue = result.Tuple[0];
            DynValue isMain = result.Tuple[1];

            Assert.That(coroutineValue.Type, Is.EqualTo(DataType.Thread));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Main));
            Assert.That(isMain.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(isMain.Boolean, Is.True);
        }

        [Test]
        public void RunningInsideCoroutineReturnsFalse()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("runningCheck"));
            DynValue result = coroutineValue.Coroutine.Resume();

            Assert.That(result.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(result.Boolean, Is.False);
        }

        [Test]
        public void StatusReflectsLifecycleAndForceSuspendedStates()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function compute()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("compute"));

            DynValue initialStatus = script.Call(statusFunc, coroutineValue);
            Assert.That(initialStatus.String, Is.EqualTo("suspended"));

            coroutineValue.Coroutine.AutoYieldCounter = 1;
            DynValue forced = coroutineValue.Coroutine.Resume();
            Assert.That(forced.Type, Is.EqualTo(DataType.YieldRequest));
            Assert.That(forced.YieldRequest.Forced, Is.True);

            DynValue suspendedStatus = script.Call(statusFunc, coroutineValue);
            Assert.That(suspendedStatus.String, Is.EqualTo("suspended"));

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();
            Assert.That(final.Type, Is.EqualTo(DataType.Number));

            DynValue deadStatus = script.Call(statusFunc, coroutineValue);
            Assert.That(deadStatus.String, Is.EqualTo("dead"));
        }

        [Test]
        public void WrapRequiresFunctionArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            Assert.That(
                () => script.Call(wrapFunc, DynValue.NewNumber(1)),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #1 to 'wrap'")
            );
        }

        [Test]
        public void WrapReturnsFunctionThatResumesCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function buildWrapper()
                    return coroutine.wrap(function()
                        for i = 1, 3 do
                            coroutine.yield(i)
                        end
                        return 'done'
                    end)
                end
            "
            );

            DynValue wrapper = script.Call(script.Globals.Get("buildWrapper"));
            Assert.That(wrapper.Type, Is.EqualTo(DataType.ClrFunction));

            DynValue first = script.Call(wrapper);
            DynValue second = script.Call(wrapper);
            DynValue third = script.Call(wrapper);
            DynValue final = script.Call(wrapper);

            Assert.Multiple(() =>
            {
                Assert.That(first.Number, Is.EqualTo(1));
                Assert.That(second.Number, Is.EqualTo(2));
                Assert.That(third.Number, Is.EqualTo(3));
                Assert.That(final.String, Is.EqualTo("done"));
            });
        }

        [Test]
        public void ResumeFlattensResultsAndReportsSuccess()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function generator()
                    coroutine.yield('yielded', 42)
                    return 7, 8
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("generator"));

            DynValue first = script.Call(resumeFunc, coroutineValue);
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(first.Tuple.Length, Is.EqualTo(3));
                Assert.That(first.Tuple[0].Boolean, Is.True);
                Assert.That(first.Tuple[1].String, Is.EqualTo("yielded"));
                Assert.That(first.Tuple[2].Number, Is.EqualTo(42));
            });

            DynValue second = script.Call(resumeFunc, coroutineValue);
            Assert.Multiple(() =>
            {
                Assert.That(second.Tuple[0].Boolean, Is.True);
                Assert.That(second.Tuple[1].Number, Is.EqualTo(7));
                Assert.That(second.Tuple[2].Number, Is.EqualTo(8));
            });
        }

        [Test]
        public void ResumeReportsErrorsAsFalseWithMessage()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function explode()
                    error('boom', 0)
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("explode"));

            DynValue result = script.Call(resumeFunc, coroutineValue);
            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(result.Tuple[1].String, Does.Contain("boom"));
            });
        }

        [Test]
        public void ResumeRequiresThreadArgument()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            Assert.That(
                () => script.Call(resumeFunc, DynValue.NewString("oops")),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #1 to 'resume'")
            );
        }
    }
}
