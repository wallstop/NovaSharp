namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ProcessorCoroutineCloseTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task CloseBeforeStartReturnsTrueAndMarksDead()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function ready() return 1 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("ready"));
            DynValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseWhileSuspendedEndsCoroutine()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function pause()
                  coroutine.yield('pause')
                  return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("pause"));
            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task CloseForceSuspendedCoroutineDrainsStack()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function slow()
                    for i = 1, 200 do end
                    return 'done'
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("slow"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(first.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task CloseAfterExceptionReturnsFalseTuple()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function blow()
                  error('boom!')
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("blow"));

            await Assert
                .That(() => coroutineValue.Coroutine.Resume())
                .Throws<ScriptRuntimeException>();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("boom");
        }

        [global::TUnit.Core.Test]
        public async Task CloseDeadCoroutineWithStoredErrorReturnsFalseTuple()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function explode()
                    error('kaboom')
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("explode"));
            await Assert
                .That(() => coroutineValue.Coroutine.Resume())
                .Throws<ScriptRuntimeException>();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            DynValue initialClose = coroutineValue.Coroutine.Close();
            await Assert.That(initialClose.Tuple[0].Boolean).IsFalse();

            DynValue subsequentClose = coroutineValue.Coroutine.Close();
            await Assert.That(subsequentClose.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(subsequentClose.Tuple[0].Boolean).IsFalse();
            await Assert.That(subsequentClose.Tuple[1].String).Contains("kaboom");
        }

        [global::TUnit.Core.Test]
        public async Task CloseMainCoroutineThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function close_main()
                    local current = select(1, coroutine.running())
                    coroutine.close(current)
                end
            "
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(script.Globals.Get("close_main"))
            );
            await Assert.That(exception.Message).Contains("attempt to close the main coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task CloseRunningCoroutineThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function close_running()
                    local worker = coroutine.create(function()
                        local current = coroutine.running()
                        coroutine.close(current)
                    end)

                    return coroutine.resume(worker)
                end
            "
            );

            DynValue result = script.Call(script.Globals.Get("close_running"));
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("cannot close a running coroutine");
        }

        [global::TUnit.Core.Test]
        public async Task CloseUnknownStateThrows()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function idle() coroutine.yield('pause') end");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));

            coroutineValue.Coroutine.ForceStateForTests((CoroutineState)0);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Close()
            );
            await Assert
                .That(exception.Message)
                .Contains("cannot close coroutine in state unknown");
        }

        [global::TUnit.Core.Test]
        public async Task CloseDeadCoroutineWithoutErrorsReturnsTrue()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString("function done() return 'ok' end");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("done"));

            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("ok");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task CloseClrCallbackCoroutineReturnsTrue()
        {
            Script script = new(CoreModulePresets.Complete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewNumber(1));
            DynValue coroutineValue = script.CreateCoroutine(callback);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);
        }

        [global::TUnit.Core.Test]
        public async Task CloseForceSuspendedCoroutineUnwindsSuccessfully()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                function heavy_close()
                    local total = 0
                    for i = 1, 500 do
                        total = total + i
                    end
                    return total
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("heavy_close"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            DynValue forcedYield = coroutineValue.Coroutine.Resume();
            await Assert.That(forcedYield.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forcedYield.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        public async Task ClosePropagatesErrorsFromCloseMetamethod()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                local function new_closable()
                    local resource = {}
                    return setmetatable(resource, {
                        __close = function(_, err)
                            error('close failure', 0)
                        end
                    })
                end

                function build_closer_coroutine()
                    return coroutine.create(function()
                        local resource <close> = new_closable()
                        coroutine.yield('pause')
                    end)
                end
            "
            );

            DynValue coroutineValue = script.Call(script.Globals.Get("build_closer_coroutine"));
            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("close failure");
        }

        [global::TUnit.Core.Test]
        public async Task CoroutineCloseFlushesAllClosersEvenWhenOneRaises()
        {
            Script script = new(CoreModulePresets.Complete);
            script.DoString(
                @"
                local log = {}

                local function new_closable(name, should_error)
                    local token = {}
                    setmetatable(token, {
                        __close = function(_, err)
                            table.insert(log, string.format('%s:%s', name, err or 'nil'))
                            if should_error then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                    return token
                end

                function build_pending_close_coroutine()
                    return coroutine.create(function()
                        local first <close> = new_closable('first', true)
                        local second <close> = new_closable('second', false)
                        coroutine.yield('pause')
                    end)
                end

                function read_close_log()
                    return log
                end
            "
            );

            DynValue coroutineValue = script.Call(
                script.Globals.Get("build_pending_close_coroutine")
            );
            DynValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");

            DynValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("close:first");

            Table log = script.Call(script.Globals.Get("read_close_log")).Table;
            await Assert.That(log.Length).IsEqualTo(2);
            await Assert.That(log.Get(1).String).IsEqualTo("second:nil");
            await Assert.That(log.Get(2).String).IsEqualTo("first:nil");
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
