namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Threading.Tasks;
    using global::NovaSharp;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ProcessorCoroutineCloseTUnitTests
    {
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseBeforeStartReturnsTrueAndMarksDead(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function ready() return 1 end");

            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("ready"));
            LuaValue closeResult = coroutineValue.Coroutine.Close();

            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseWhileSuspendedEndsCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function pause()
                  coroutine.yield('pause')
                  return 'done'
                end
            "
            );

            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("pause"));
            LuaValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Suspended);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Resume()
            );
            await Assert.That(exception.Message).Contains("cannot resume dead coroutine");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseForceSuspendedCoroutineDrainsStack(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function slow()
                    for i = 1, 200 do end
                    return 'done'
                end
            "
            );

            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("slow"));
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            LuaValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(first.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseAfterExceptionReturnsFalseTuple(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function blow()
                  error('boom!')
                end
            "
            );

            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("blow"));

            await Assert
                .That(() => coroutineValue.Coroutine.Resume())
                .Throws<ScriptRuntimeException>();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("boom");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseDeadCoroutineWithStoredErrorReportsItOnce(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function explode()
                    error('kaboom')
                end
            "
            );

            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("explode"));
            await Assert
                .That(() => coroutineValue.Coroutine.Resume())
                .Throws<ScriptRuntimeException>();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            LuaValue initialClose = coroutineValue.Coroutine.Close();
            await Assert.That(initialClose.Tuple[0].Boolean).IsFalse();
            await Assert.That(initialClose.Tuple[1].String).Contains("kaboom");

            LuaValue subsequentClose = coroutineValue.Coroutine.Close();
            await Assert.That(subsequentClose.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(subsequentClose.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task UncaughtErrorDefersCloseHandlersUntilClose(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local seen = {}
                local function make(name)
                    return setmetatable({}, {
                        __close = function(_, err)
                            seen[#seen + 1] = name .. ':' .. tostring(err)
                        end
                    })
                end

                local task = coroutine.create(function()
                    local explicit <close> = make('explicit')
                    for _ in function() error('boom', 0) end, nil, nil, make('generic') do end
                end)

                local ok, err = coroutine.resume(task)
                local before = #seen
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', ok, err, before)
                print('close', closed, closeError, #seen, seen[1], seen[2])
                print('close2', closedAgain, closeErrorAgain, #seen)

                return ok, err, before,
                    closed, closeError, #seen, seen[1], seen[2],
                    closedAgain, closeErrorAgain
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).IsEqualTo("boom");
            await Assert.That(result.Tuple[2].Number).IsEqualTo(0);
            await Assert.That(result.Tuple[3].Boolean).IsFalse();
            await Assert.That(result.Tuple[4].String).IsEqualTo("boom");
            await Assert.That(result.Tuple[5].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[6].String).IsEqualTo("generic:boom");
            await Assert.That(result.Tuple[7].String).IsEqualTo("explicit:boom");
            await Assert.That(result.Tuple[8].Boolean).IsTrue();
            await Assert.That(result.Tuple[9].IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task DeferredCloseHandlerErrorReplacesActiveErrorAndClosesOnce(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'outer:' .. tostring(err)
                        end
                    })
                    local inner <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'inner:' .. tostring(err)
                            error('closeboom', 0)
                        end
                    })
                    error('original', 0)
                end)

                local ok, err = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', ok, err)
                print('close', closed, closeError, #log, log[1], log[2])
                print('close2', closedAgain, closeErrorAgain)

                return ok, err, closed, closeError,
                    #log, log[1], log[2], closedAgain, closeErrorAgain
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).IsEqualTo("original");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("closeboom");
            await Assert.That(result.Tuple[4].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[5].String).IsEqualTo("inner:original");
            await Assert.That(result.Tuple[6].String).IsEqualTo("outer:closeboom");
            await Assert.That(result.Tuple[7].Boolean).IsTrue();
            await Assert.That(result.Tuple[8].IsNil).IsTrue();
        }

        /// <summary>
        /// Tests that coroutine.close() on the main coroutine throws an error.
        /// Note: coroutine.close() is a Lua 5.4+ function.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseMainCoroutineThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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

        /// <summary>
        /// Tests that coroutine.close() on a running coroutine throws an error.
        /// Note: coroutine.close() is a Lua 5.4+ function.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseRunningCoroutineThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            LuaValue result = script.Call(script.Globals.Get("close_running"));
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("cannot close a running coroutine");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseUnknownStateThrows(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function idle() coroutine.yield('pause') end");
            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("idle"));

            coroutineValue.Coroutine.ForceStateForTests((CoroutineState)0);

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                coroutineValue.Coroutine.Close()
            );
            await Assert
                .That(exception.Message)
                .Contains("cannot close coroutine in state unknown");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseDeadCoroutineWithoutErrorsReturnsTrue(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString("function done() return 'ok' end");
            LuaValue coroutineValue = script.CreateCoroutineValue(script.Globals.Get("done"));

            LuaValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("ok");
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseClrCallbackCoroutineReturnsTrue(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue callback = LuaValue.NewCallback((_, _) => LuaValue.NewNumber(1));
            LuaValue coroutineValue = script.CreateCoroutineValue(callback);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.NotStarted);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseForceSuspendedCoroutineUnwindsSuccessfully(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            LuaValue coroutineValue = script.CreateCoroutineValue(
                script.Globals.Get("heavy_close")
            );
            coroutineValue.Coroutine.AutoYieldCounter = 1;

            LuaValue forcedYield = coroutineValue.Coroutine.Resume();
            await Assert.That(forcedYield.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forcedYield.YieldRequest.Forced).IsTrue();
            await Assert
                .That(coroutineValue.Coroutine.State)
                .IsEqualTo(CoroutineState.ForceSuspended);

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(closeResult.Boolean).IsTrue();
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Dead);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ClosePropagatesErrorsFromCloseMetamethod(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            LuaValue coroutineValue = script.Call(script.Globals.Get("build_closer_coroutine"));
            LuaValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("close failure");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoroutineCloseFlushesAllClosersEvenWhenOneRaises(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            LuaValue coroutineValue = script.Call(
                script.Globals.Get("build_pending_close_coroutine")
            );
            LuaValue first = coroutineValue.Coroutine.Resume();
            await Assert.That(first.String).IsEqualTo("pause");

            LuaValue closeResult = coroutineValue.Coroutine.Close();
            await Assert.That(closeResult.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(closeResult.Tuple[0].Boolean).IsFalse();
            await Assert.That(closeResult.Tuple[1].String).Contains("close:first");

            Table log = script.Call(script.Globals.Get("read_close_log")).Table;
            await Assert.That(log.Length).IsEqualTo(2);
            await Assert.That(log.Get(1).String).IsEqualTo("second:nil");
            await Assert.That(log.Get(2).String).IsEqualTo("first:nil");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SuspendedCloseContinuesThroughOuterCloseHandlers(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local function make(name, shouldError)
                    return setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = name .. ':' .. tostring(err)
                            if shouldError then
                                error('close:' .. name, 0)
                            end
                        end
                    })
                end

                local function inner()
                    local pending <close> = make('inner', true)
                    coroutine.yield('pause')
                end

                local task = coroutine.create(function()
                    local outer <close> = make('outer', false)
                    inner()
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2])
                print('close2', closedAgain, closeErrorAgain, #log)

                return resumed, paused, closed, closeError,
                    #log, log[1], log[2], closedAgain, closeErrorAgain
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("close:inner");
            await Assert.That(result.Tuple[4].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[5].String).IsEqualTo("inner:nil");
            await Assert.That(result.Tuple[6].String).IsEqualTo("outer:close:inner");
            await Assert.That(result.Tuple[7].Boolean).IsTrue();
            await Assert.That(result.Tuple[8].IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseHandlerErrorUnwindsItsOwnScopeBeforeSiblings(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local function make(name)
                    return setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = name .. ':' .. tostring(err)
                        end
                    })
                end

                local task = coroutine.create(function()
                    local sibling <close> = make('sibling')
                    local failing <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'failing:' .. tostring(err)
                            local inner <close> = make('inner')
                            error('handlerboom', 0)
                        end
                    })
                    error('original', 0)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError)
                print('close', closed, closeError, #log, log[1], log[2], log[3])

                return resumed, resumeError, closed, closeError,
                    #log, log[1], log[2], log[3]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).IsEqualTo("original");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("handlerboom");
            await Assert.That(result.Tuple[4].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[5].String).IsEqualTo("failing:original");
            await Assert.That(result.Tuple[6].String).IsEqualTo("inner:handlerboom");
            await Assert.That(result.Tuple[7].String).IsEqualTo("sibling:handlerboom");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseDuringDeferredCleanupResetsActiveError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local sibling <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'sibling:' .. tostring(err)
                        end
                    })
                    local failing <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'failing:' .. tostring(err)
                            local inner <close> = setmetatable({}, {
                                __close = function(_, innerError)
                                    log[#log + 1] = 'inner:' .. tostring(innerError)
                                end
                            })
                            coroutine.close(task)
                            log[#log + 1] = 'after'
                        end
                    })
                    error('original', 0)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError)
                print('close', closed, closeError, #log, log[1], log[2], log[3])

                return resumed, resumeError, closed, closeError,
                    #log, log[1], log[2], log[3]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).IsEqualTo("original");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert.That(result.Tuple[4].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[5].String).IsEqualTo("failing:original");
            await Assert.That(result.Tuple[6].String).IsEqualTo("inner:nil");
            await Assert.That(result.Tuple[7].String).IsEqualTo("sibling:nil");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideNestedBlockDoesNotResumeRemovedFrame(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local inner <close> = setmetatable({}, {
                                    __close = function()
                                        log[#log + 1] = 'inner'
                                        coroutine.close(task)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, paused, closed, closeError, #log, log[1], log[2]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert.That(result.Tuple[4].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[5].String).IsEqualTo("outer-start");
            await Assert.That(result.Tuple[6].String).IsEqualTo("inner");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideNestedBlockDrainsRemainingSiblings(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local a <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function()
                                        log[#log + 1] = 'b'
                                        coroutine.close(task)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError ~= nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError ~= nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsEqualTo(isLua55);
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(!isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(isLua55 ? "outer-start,b,a:nil" : "outer-start,b,a:error");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseAfterNestedBlockErrorPreservesCloseFailure(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'outer:' .. (err == nil and 'nil' or 'error')
                            coroutine.close(task)
                        end
                    })
                    do
                        local inner <close> = setmetatable({}, {
                            __close = function(_, err)
                                log[#log + 1] = 'inner:' .. (err == nil and 'nil' or 'error')
                                error('innerboom', 0)
                            end
                        })
                        coroutine.yield('pause')
                    end
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert.That(result.Tuple[4].String).IsEqualTo("inner:nil,outer:error");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseErrorInsideProtectedCallDoesNotFailClose(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            local ok, err = pcall(function()
                                local inner <close> = setmetatable({}, {
                                    __close = function()
                                        log[#log + 1] = 'inner'
                                        error('innerboom', 0)
                                    end
                                })
                            end)
                            log[#log + 1] = 'pcall:' .. tostring(ok) .. ':' .. tostring(err)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Boolean).IsTrue();
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo("outer-start,inner,pcall:false:innerboom");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideProtectedCallAbortsHandler(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            local ok, err = pcall(function()
                                local a <close> = setmetatable({}, {
                                    __close = function(_, innerError)
                                        log[#log + 1] = 'a:'
                                            .. (innerError == nil and 'nil' or 'error')
                                        error('aboom', 0)
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function(_, innerError)
                                        log[#log + 1] = 'b:'
                                            .. (innerError == nil and 'nil' or 'error')
                                        coroutine.close(task)
                                    end
                                })
                            end)
                            log[#log + 1] = 'pcall:' .. tostring(ok) .. ':' .. tostring(err)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsEqualTo(!isLua55);
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(!isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer-start,b:nil,a:nil"
                        : "outer-start,b:nil,a:error,pcall:false:aboom"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideSuspendedProtectedCallKeepsFailure(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    return pcall(function()
                        local outer <close> = setmetatable({}, {
                            __close = function()
                                log[#log + 1] = 'outer-start'
                                do
                                    local a <close> = setmetatable({}, {
                                        __close = function(_, err)
                                            log[#log + 1] = 'a:'
                                                .. (err == nil and 'nil' or 'error')
                                        end
                                    })
                                    local b <close> = setmetatable({}, {
                                        __close = function(_, err)
                                            log[#log + 1] = 'b:'
                                                .. (err == nil and 'nil' or 'error')
                                            local nested <close> = setmetatable({}, {
                                                __close = function(_, nestedError)
                                                    log[#log + 1] = 'nested:'
                                                        .. (nestedError == nil and 'nil' or 'error')
                                                    coroutine.close(task)
                                                end
                                            })
                                            log[#log + 1] = 'b-after'
                                        end
                                    })
                                    local c <close> = setmetatable({}, {
                                        __close = function(_, err)
                                            log[#log + 1] = 'c:'
                                                .. (err == nil and 'nil' or 'error')
                                            error('cboom', 0)
                                        end
                                    })
                                end
                                log[#log + 1] = 'outer-after'
                            end
                        })
                        coroutine.yield('pause')
                    end)
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer-start,c:nil,b:error,b-after,nested:nil,a:nil"
                        : "outer-start,c:nil,b:error,b-after,nested:nil,a:error"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideNestedCloserKeepsAncestorFailure(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local a <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                        local nested <close> = setmetatable({}, {
                                            __close = function(_, nestedError)
                                                log[#log + 1] = 'nested:'
                                                    .. (nestedError == nil and 'nil' or 'error')
                                                coroutine.close(task)
                                            end
                                        })
                                        log[#log + 1] = 'b-after'
                                    end
                                })
                                local c <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'c:' .. (err == nil and 'nil' or 'error')
                                        error('cboom', 0)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer-start,c:nil,b:error,b-after,nested:nil,a:nil"
                        : "outer-start,c:nil,b:error,b-after,nested:nil,a:error"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseInsideNestedHandlerKeepsAncestorFrameAlive(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer'
                            local a <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                end
                            })
                            local b <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                    local c <close> = setmetatable({}, {
                                        __close = function(_, innerError)
                                            log[#log + 1] = 'c:'
                                                .. (innerError == nil and 'nil' or 'error')
                                            coroutine.close(task)
                                        end
                                    })
                                    log[#log + 1] = 'b-after'
                                end
                            })
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsEqualTo(isLua55);
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer,b:nil,b-after,c:nil,a:nil"
                        : "outer,b:nil,b-after,c:nil,a:error"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseDuringDrainClearsLaterError(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local a <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                        coroutine.close(task)
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                        error('bboom', 0)
                                    end
                                })
                                local c <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'c:' .. (err == nil and 'nil' or 'error')
                                        coroutine.close(task)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsEqualTo(isLua55);
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer-start,c:nil,b:nil,a:error"
                        : "outer-start,c:nil,b:error,a:error"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseAfterPriorHandlerErrorResetsCloserError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local outer <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'outer-start'
                            do
                                local a <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'a:' .. (err == nil and 'nil' or 'error')
                                    end
                                })
                                local b <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'b:' .. (err == nil and 'nil' or 'error')
                                        coroutine.close(task)
                                    end
                                })
                                local c <close> = setmetatable({}, {
                                    __close = function(_, err)
                                        log[#log + 1] = 'c:' .. (err == nil and 'nil' or 'error')
                                        error('cboom', 0)
                                    end
                                })
                            end
                            log[#log + 1] = 'outer-after'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError == nil)
                print('log', table.concat(log, ','))

                return resumed, paused, closed, closeError == nil, table.concat(log, ',')
                "
            );

            bool isLua55 = version == LuaCompatibilityVersion.Lua55;
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsEqualTo(isLua55);
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo(
                    isLua55
                        ? "outer-start,c:nil,b:error,a:nil"
                        : "outer-start,c:nil,b:error,a:error"
                );
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseClearsPriorHandlerErrorBeforeSiblings(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local first <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'first:' .. tostring(err)
                            error('firstboom', 0)
                        end
                    })
                    local second <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'second:' .. tostring(err)
                            coroutine.close(task)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, paused, closed, closeError, #log, log[1], log[2]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("firstboom");
            await Assert.That(result.Tuple[4].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[5].String).IsEqualTo("second:nil");
            await Assert.That(result.Tuple[6].String).IsEqualTo("first:nil");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task SelfCloseDuringDeferredCleanupKeepsInnerHandlerError(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local failing <close> = setmetatable({}, {
                        __close = function(_, err)
                            log[#log + 1] = 'failing:' .. tostring(err)
                            local inner <close> = setmetatable({}, {
                                __close = function(_, innerError)
                                    log[#log + 1] = 'inner:' .. tostring(innerError)
                                    error('innerboom', 0)
                                end
                            })
                            coroutine.close(task)
                        end
                    })
                    error('original', 0)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, resumeError)
                print('close', closed, closeError, #log, log[1], log[2])

                return resumed, resumeError, closed, closeError,
                    #log, log[1], log[2]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).IsEqualTo("original");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("innerboom");
            await Assert.That(result.Tuple[4].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[5].String).IsEqualTo("failing:original");
            await Assert.That(result.Tuple[6].String).IsEqualTo("inner:nil");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task OtherCoroutineCannotCloseCoroutineDuringHandlers(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local first, second
                local rejected = false
                second = coroutine.create(function()
                    local ok, err = pcall(function() return coroutine.close(first) end)
                    rejected = ok == false
                        and type(err) == 'string'
                        and err:find('cannot close a normal coroutine', 1, true) ~= nil
                end)

                first = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            coroutine.resume(second)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(first)
                local closed, closeError = coroutine.close(first)

                print('resume', resumed, paused)
                print('close', closed, closeError, rejected)

                return resumed, paused, closed, closeError, rejected
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert.That(result.Tuple[4].Boolean).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseHandlerSelfCloseAbortsHandlerAndContinuesInLua55(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local log = {}
                local task
                task = coroutine.create(function()
                    local first <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'first'
                            local inner <close> = setmetatable({}, {
                                __close = function(_, err)
                                    log[#log + 1] = 'inner:' .. tostring(err)
                                end
                            })
                            coroutine.close(task)
                            log[#log + 1] = 'after'
                        end
                    })
                    local second <close> = setmetatable({}, {
                        __close = function()
                            log[#log + 1] = 'second'
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, #log, log[1], log[2], log[3])

                return resumed, paused, closed, closeError,
                    #log, log[1], log[2], log[3]
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert.That(result.Tuple[4].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[5].String).IsEqualTo("second");
            await Assert.That(result.Tuple[6].String).IsEqualTo("first");
            await Assert.That(result.Tuple[7].String).IsEqualTo("inner:nil");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        public async Task CloseHandlerCannotReenterCloseOfItsOwnCoroutine(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            coroutine.close(task)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)
                local rejected = type(closeError) == 'string'
                    and closeError:find('cannot close a running coroutine', 1, true) ~= nil
                local closedAgain, closeErrorAgain = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, rejected)
                print('close2', closedAgain, closeErrorAgain)

                return resumed, paused, closed, rejected, closedAgain, closeErrorAgain
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].Boolean).IsTrue();
            await Assert.That(result.Tuple[4].Boolean).IsTrue();
            await Assert.That(result.Tuple[5].IsNil).IsTrue();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseHandlerCannotResumeItsOwnCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local observed
                local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            local resumed, value, message = pcall(coroutine.resume, task)
                            observed = tostring(resumed) .. ':' .. tostring(value) .. ':' .. tostring(message)
                        end
                    })
                    coroutine.yield('pause')
                end)

                local resumed, paused = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, paused)
                print('close', closed, closeError, observed)

                return resumed, paused, closed, closeError, observed
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("pause");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert
                .That(result.Tuple[4].String)
                .IsEqualTo("true:false:cannot resume non-suspended coroutine");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CloseHandlerObservesRunningCoroutineState(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local observedStatus, observedYieldable
                local task
                task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            observedStatus = coroutine.status(task)
                            observedYieldable = coroutine.isyieldable()
                        end
                    })
                    error('boom', 0)
                end)

                local resumed = coroutine.resume(task)
                local closed = coroutine.close(task)

                print('states', resumed, closed, observedStatus, observedYieldable)

                return resumed, closed, observedStatus, observedYieldable
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].Boolean).IsFalse();
            await Assert.That(result.Tuple[2].String).IsEqualTo("running");
            await Assert.That(result.Tuple[3].Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task FailedResumeKeepsDeferredCloseError(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local original = {}
                local seen
                local task = coroutine.create(function()
                    local pending <close> = setmetatable({}, {
                        __close = function(_, err) seen = err end
                    })
                    error(original)
                end)

                local resumed, resumeError = coroutine.resume(task)
                local resumedAgain, resumeErrorAgain = coroutine.resume(task)
                local closed, closeError = coroutine.close(task)

                print('resume', resumed, rawequal(original, resumeError))
                print('retry', resumedAgain, type(resumeErrorAgain))
                print('close', closed, rawequal(original, seen), rawequal(original, closeError))

                return resumed, rawequal(original, resumeError),
                    resumedAgain, type(resumeErrorAgain),
                    closed, rawequal(original, seen), rawequal(original, closeError)
                "
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].Boolean).IsTrue();
            await Assert.That(result.Tuple[2].Boolean).IsFalse();
            await Assert.That(result.Tuple[3].String).IsEqualTo("string");
            await Assert.That(result.Tuple[4].Boolean).IsFalse();
            await Assert.That(result.Tuple[5].Boolean).IsTrue();
            await Assert.That(result.Tuple[6].Boolean).IsTrue();
        }

        // =====================================================
        // Negative Tests: coroutine.close() not available pre-5.4
        // =====================================================

        /// <summary>
        /// Tests that coroutine.close is nil in Lua 5.1-5.3.
        /// The coroutine.close() function was introduced in Lua 5.4.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        public async Task CoroutineCloseIsNilInPreLua54(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue closeFunc = script.DoString("return coroutine.close");

            await Assert
                .That(closeFunc.Type)
                .IsEqualTo(DataType.Nil)
                .Because($"coroutine.close should be nil in {version}, got {closeFunc.Type}");
        }

        /// <summary>
        /// Tests that coroutine.close is available in Lua 5.4+.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CoroutineCloseIsAvailableInLua54Plus(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            LuaValue closeFunc = script.DoString("return coroutine.close");

            await Assert
                .That(closeFunc.Type)
                .IsEqualTo(DataType.ClrFunction)
                .Because(
                    $"coroutine.close should be a function in {version}, got {closeFunc.Type}"
                );
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
