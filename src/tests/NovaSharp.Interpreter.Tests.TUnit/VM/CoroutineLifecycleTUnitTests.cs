namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

    public sealed class CoroutineLifecycleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ResumeAfterCompletionThrowsCannotResumeNotSuspended()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function simple() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("simple"));

            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.Type).IsEqualTo(DataType.Number);
            await Assert.That(first.Number).IsEqualTo(5);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineCreatesReusableInstance()
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
            await Assert.That(initial.String).IsEqualTo("done");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            DynValue recycledValue = script.RecycleCoroutine(
                coroutineValue.Coroutine,
                script.Globals.Get("second")
            );

            await Assert
                .That(coroutineValue.Coroutine.Type)
                .IsEqualTo(Coroutine.CoroutineType.Recycled);

            Coroutine recycled = recycledValue.Coroutine;
            await Assert.That(recycled.State).IsEqualTo(CoroutineState.NotStarted);

            DynValue firstYield = recycled.Resume();
            await Assert.That(firstYield.String).IsEqualTo("pause");
            await Assert.That(recycled.State).IsEqualTo(CoroutineState.Suspended);

            DynValue final = recycled.Resume();
            await Assert.That(final.String).IsEqualTo("done-again");
            await Assert.That(recycled.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task RecycleCoroutineThrowsWhenNotDead()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function sample() coroutine.yield(1) end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("sample"));
            coroutineValue.Coroutine.Resume();

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                script.RecycleCoroutine(coroutineValue.Coroutine, script.Globals.Get("sample"))
            );
            await Assert.That(exception.Message).Contains("state must be CoroutineState.Dead");
        }

        [global::TUnit.Core.Test]
        public async Task AutoYieldCounterForcesYieldAndResumesCleanly()
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(first.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();

            await Assert.That(final.Type).IsEqualTo(DataType.Number);
            await Assert.That(final.Number).IsEqualTo(400 * 401 / 2);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead()
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest);
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            ArgumentException exception = ExpectException<ArgumentException>(() =>
                coroutineValue.Coroutine.Resume(DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("args must be empty");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ForceSuspendedCoroutineResumesWithContextWithoutArguments()
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
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(first.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue final = coroutineValue.Coroutine.Resume(context);

            await Assert.That(final.Type).IsEqualTo(DataType.Number);
            await Assert.That(final.Number).IsEqualTo(300 * 301 / 2);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task SuspendedCoroutineReceivesResumeArguments()
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
            await Assert.That(first.Type).IsEqualTo(DataType.String);
            await Assert.That(first.String).IsEqualTo("ready");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            DynValue result = coroutineValue.Coroutine.Resume(DynValue.NewNumber(21));

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(42);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseSuspendedCoroutineReturnsTrue()
        {
            Script script = new(CoreModules.PresetComplete);
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
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseSuspendedCoroutinePropagatesErrors()
        {
            Script script = new(CoreModules.PresetComplete);
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
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("close-fail");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseNotStartedCoroutineReturnsTrue()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function never_started() return 5 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("never_started"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseDeadCoroutineReturnsLastErrorTuple()
        {
            Script script = new(CoreModules.PresetComplete);
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
            await Assert.That(firstClose.Tuple[0].Boolean).IsFalse();

            DynValue secondClose = coroutineValue.Coroutine.Close();

            await Assert.That(secondClose.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(secondClose.Tuple[0].Boolean).IsFalse();
            await Assert.That(secondClose.Tuple[1].String).Contains("close-dead");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        private static TException ExpectException<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
