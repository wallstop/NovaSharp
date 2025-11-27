namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineLifecycleTests
    {
        [Test]
        public void ResumeAfterCompletionThrowsCannotResumeNotSuspended()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function simple() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("simple"));

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.That(first.Type, Is.EqualTo(DataType.Number));
            Assert.That(first.Number, Is.EqualTo(5));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));

            Assert.That(
                () => coroutineValue.Coroutine.Resume(),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot resume dead coroutine")
            );
        }

        [Test]
        public void RecycleCoroutineCreatesReusableInstance()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function first()
                    return 'done'
                end

                function second()
                    coroutine.yield('pause')
                    return 'done-again'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("first"));

            DynValue initial = coroutineValue.Coroutine.Resume();
            Assert.That(initial.String, Is.EqualTo("done"));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));

            DynValue recycledValue = script.RecycleCoroutine(
                coroutineValue.Coroutine,
                script.Globals.Get("second")
            );

            Assert.That(
                coroutineValue.Coroutine.Type,
                Is.EqualTo(Coroutine.CoroutineType.Recycled)
            );

            Coroutine recycled = recycledValue.Coroutine;
            Assert.That(recycled.State, Is.EqualTo(CoroutineState.NotStarted));

            DynValue firstYield = recycled.Resume();
            Assert.That(firstYield.String, Is.EqualTo("pause"));
            Assert.That(recycled.State, Is.EqualTo(CoroutineState.Suspended));

            DynValue final = recycled.Resume();
            Assert.That(final.String, Is.EqualTo("done-again"));
            Assert.That(recycled.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void RecycleCoroutineThrowsWhenNotDead()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function sample() coroutine.yield(1) end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("sample"));
            coroutineValue.Coroutine.Resume(); // suspend, state is Suspended

            Assert.That(
                () =>
                    script.RecycleCoroutine(coroutineValue.Coroutine, script.Globals.Get("sample")),
                Throws
                    .TypeOf<InvalidOperationException>()
                    .With.Message.Contains("state must be CoroutineState.Dead")
            );
        }

        [Test]
        public void AutoYieldCounterForcesYieldAndResumesCleanly()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function heavy()
                    local sum = 0
                    for i = 1, 400 do
                        sum = sum + i
                    end
                    return sum
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("heavy"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.YieldRequest));
                Assert.That(first.YieldRequest.Forced, Is.True);
                Assert.That(
                    coroutineValue.Coroutine.State,
                    Is.EqualTo(CoroutineState.ForceSuspended)
                );
            });

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();

            Assert.Multiple(() =>
            {
                Assert.That(final.Type, Is.EqualTo(DataType.Number));
                Assert.That(final.Number, Is.EqualTo(400 * 401 / 2));
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function busy()
                    for i = 1, 200 do end
                    return 'finished'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("busy"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.That(first.Type, Is.EqualTo(DataType.YieldRequest));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.ForceSuspended));

            Assert.That(
                () => coroutineValue.Coroutine.Resume(DynValue.NewNumber(1)),
                Throws.TypeOf<ArgumentException>().With.Message.Contains("args must be empty")
            );

            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
        }

        [Test]
        public void ForceSuspendedCoroutineResumesWithContextWithoutArguments()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function heavyweight()
                    local total = 0
                    for i = 1, 300 do
                        total = total + i
                    end
                    return total
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("heavyweight"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.YieldRequest));
                Assert.That(first.YieldRequest.Forced, Is.True);
                Assert.That(
                    coroutineValue.Coroutine.State,
                    Is.EqualTo(CoroutineState.ForceSuspended)
                );
            });

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue final = coroutineValue.Coroutine.Resume(context);

            Assert.Multiple(() =>
            {
                Assert.That(final.Type, Is.EqualTo(DataType.Number));
                Assert.That(final.Number, Is.EqualTo(300 * 301 / 2));
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void SuspendedCoroutineReceivesResumeArguments()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function accumulator()
                    local first = coroutine.yield('ready')
                    return first * 2
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("accumulator"));

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.Multiple(() =>
            {
                Assert.That(first.Type, Is.EqualTo(DataType.String));
                Assert.That(first.String, Is.EqualTo("ready"));
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));
            });

            DynValue result = coroutineValue.Coroutine.Resume(DynValue.NewNumber(21));

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(42));
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }
    }
}
