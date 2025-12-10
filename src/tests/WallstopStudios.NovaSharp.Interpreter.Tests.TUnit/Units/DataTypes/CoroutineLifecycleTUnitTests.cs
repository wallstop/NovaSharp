namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class CoroutineLifecycleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ResumeAfterCompletionThrowsCannotResumeNotSuspended()
        {
            Script script = CreateScript();
            script.DoString("function simple() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("simple"));

            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(first.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );

            await Assert
                .That(exception.Message)
                .Contains("cannot resume dead coroutine")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineCreatesReusableInstance()
        {
            Script script = CreateScript();
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
            await Assert.That(initial.String).IsEqualTo("done").ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);

            DynValue recycledValue = script.RecycleCoroutine(
                coroutineValue.Coroutine,
                script.Globals.Get("second")
            );

            await Assert
                .That(coroutineValue.Coroutine.Type)
                .IsEqualTo(Coroutine.CoroutineType.Recycled)
                .ConfigureAwait(false);

            Coroutine recycled = recycledValue.Coroutine;
            await Assert
                .That(recycled.State)
                .IsEqualTo(CoroutineState.NotStarted)
                .ConfigureAwait(false);

            DynValue firstYield = recycled.Resume();
            await Assert.That(firstYield.String).IsEqualTo("pause").ConfigureAwait(false);
            await Assert
                .That(recycled.State)
                .IsEqualTo(CoroutineState.Suspended)
                .ConfigureAwait(false);

            DynValue final = recycled.Resume();
            await Assert.That(final.String).IsEqualTo("done-again").ConfigureAwait(false);
            await Assert.That(recycled.State).IsEqualTo(CoroutineState.Dead).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineThrowsWhenNotDead()
        {
            Script script = CreateScript();
            script.DoString("function sample() coroutine.yield(1) end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("sample"));
            coroutineValue.Coroutine.Resume();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                script.RecycleCoroutine(coroutineValue.Coroutine, script.Globals.Get("sample"))
            );

            await Assert
                .That(exception.Message)
                .Contains("state must be CoroutineState.Dead")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task AutoYieldCounterForcesYieldAndResumesCleanly()
        {
            Script script = CreateScript();
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest).ConfigureAwait(false);
            await Assert.That(first.YieldRequest.Forced).IsTrue().ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended)
                .ConfigureAwait(false);

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();

            await Assert.That(final.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(final.Number).IsEqualTo(400d * 401d / 2d).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead()
        {
            Script script = CreateScript();
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended)
                .ConfigureAwait(false);

            ArgumentException exception = Assert.Throws<ArgumentException>(() =>
                coroutineValue.Coroutine.Resume(DynValue.NewNumber(1))
            );

            await Assert
                .That(exception.Message)
                .Contains("args must be empty")
                .ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task ForceSuspendedCoroutineResumesWithContextWithoutArguments()
        {
            Script script = CreateScript();
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest).ConfigureAwait(false);
            await Assert.That(first.YieldRequest.Forced).IsTrue().ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended)
                .ConfigureAwait(false);

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue final = coroutineValue.Coroutine.Resume(context);

            await Assert.That(final.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(final.Number).IsEqualTo(300d * 301d / 2d).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task SuspendedCoroutineReceivesResumeArguments()
        {
            Script script = CreateScript();
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
            await Assert.That(first.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(first.String).IsEqualTo("ready").ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Suspended)
                .ConfigureAwait(false);

            DynValue result = coroutineValue.Coroutine.Resume(DynValue.NewNumber(21));

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(42d).ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CloseSuspendedCoroutineReturnsTrue()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function closable_success()
                    local handle <close> = setmetatable({}, { __close = function() end })
                    coroutine.yield('pause')
                end
                "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("closable_success")
            );
            coroutineValue.Coroutine.Resume();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Suspended)
                .ConfigureAwait(false);

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(closeResult.Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CloseSuspendedCoroutinePropagatesErrors()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-fail') end
                    })
                    coroutine.yield('pause')
                end
                "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("closable_failure")
            );
            coroutineValue.Coroutine.Resume();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Suspended)
                .ConfigureAwait(false);

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(closeResult.Tuple[1].String)
                .Contains("close-fail")
                .ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CloseNotStartedCoroutineReturnsTrue()
        {
            Script script = CreateScript();
            script.DoString("function never_started() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("never_started"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean).ConfigureAwait(false);
            await Assert.That(closeResult.Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task CloseDeadCoroutineReturnsLastErrorTuple()
        {
            Script script = CreateScript();
            script.DoString(
                @"
                function closable_failure()
                    local handle <close> = setmetatable({}, {
                        __close = function() error('close-dead') end
                    })
                    coroutine.yield()
                end
                "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("closable_failure")
            );
            coroutineValue.Coroutine.Resume();

            DynValue firstClose = coroutineValue.Coroutine.Close();
            await Assert.That(firstClose.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);

            DynValue secondClose = coroutineValue.Coroutine.Close();

            await Assert.That(secondClose.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(secondClose.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(secondClose.Tuple[1].String)
                .Contains("close-dead")
                .ConfigureAwait(false);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.Dead)
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }
    }
}
