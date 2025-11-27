namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class CoroutineCloseTests
    {
        [Test]
        public void CloseBeforeStartReturnsTrueAndMarksDead()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function ready() return 1 end");

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("ready"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void CloseWhileSuspendedEndsCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.That(first.Type, Is.EqualTo(DataType.String));
            Assert.That(first.String, Is.EqualTo("pause"));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Suspended));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });

            Assert.That(
                () => coroutineValue.Coroutine.Resume(),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot resume dead coroutine")
            );
        }

        [Test]
        public void CloseAfterExceptionReturnsFalseTuple()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function blow()
                  error('boom!')
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("blow"));

            Assert.That(
                () => coroutineValue.Coroutine.Resume(),
                Throws.TypeOf<ScriptRuntimeException>().With.Message.Contains("boom")
            );
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(closeResult.Tuple.Length, Is.EqualTo(2));
                Assert.That(closeResult.Tuple[0].Boolean, Is.False);
                Assert.That(closeResult.Tuple[1].String, Does.Contain("boom"));
            });
        }

        [Test]
        public void CloseMainCoroutineThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString(
                @"
                function close_main()
                    local current = select(1, coroutine.running())
                    coroutine.close(current)
                end
            "
            );

            Assert.That(
                () => script.Call(script.Globals.Get("close_main")),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("attempt to close the main coroutine")
            );
        }

        [Test]
        public void CloseRunningCoroutineThrows()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.GreaterThanOrEqualTo(2));
                Assert.That(result.Tuple[0].Boolean, Is.False);
                Assert.That(
                    result.Tuple[1].String,
                    Does.Contain("cannot close a running coroutine")
                );
            });
        }

        [Test]
        public void CloseUnknownStateThrows()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function idle() coroutine.yield('pause') end");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));

            coroutineValue.Coroutine.ForceStateForTests((CoroutineState)0);

            Assert.That(
                () => coroutineValue.Coroutine.Close(),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot close coroutine in state unknown")
            );
        }

        [Test]
        public void CloseDeadCoroutineWithoutErrorsReturnsTrue()
        {
            Script script = new(CoreModules.PresetComplete);
            script.DoString("function done() return 'ok' end");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("done"));

            DynValue first = coroutineValue.Coroutine.Resume();
            Assert.That(first.String, Is.EqualTo("ok"));
            Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
            Assert.That(closeResult.Boolean, Is.True);
        }

        [Test]
        public void CloseClrCallbackCoroutineReturnsTrue()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback((_, _) => DynValue.NewNumber(1));
            DynValue coroutineValue = script.CreateCoroutine(callback);

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.NotStarted));
            });
        }

        [Test]
        public void CloseForceSuspendedCoroutineUnwindsSuccessfully()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.Multiple(() =>
            {
                Assert.That(forcedYield.Type, Is.EqualTo(DataType.YieldRequest));
                Assert.That(forcedYield.YieldRequest.Forced, Is.True);
                Assert.That(
                    coroutineValue.Coroutine.State,
                    Is.EqualTo(CoroutineState.ForceSuspended)
                );
            });

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Boolean));
                Assert.That(closeResult.Boolean, Is.True);
                Assert.That(coroutineValue.Coroutine.State, Is.EqualTo(CoroutineState.Dead));
            });
        }

        [Test]
        public void ClosePropagatesErrorsFromCloseMetamethod()
        {
            Script script = new(CoreModules.PresetComplete);
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

            Assert.That(first.String, Is.EqualTo("pause"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(closeResult.Tuple[0].Boolean, Is.False);
                Assert.That(closeResult.Tuple[1].String, Does.Contain("close failure"));
            });
        }

        [Test]
        public void CoroutineCloseFlushesAllClosersEvenWhenOneRaises()
        {
            Script script = new(CoreModules.PresetComplete);
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
            Assert.That(first.Type, Is.EqualTo(DataType.String));
            Assert.That(first.String, Is.EqualTo("pause"));

            DynValue closeResult = coroutineValue.Coroutine.Close();

            Assert.Multiple(() =>
            {
                Assert.That(closeResult.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(closeResult.Tuple[0].Boolean, Is.False);
                Assert.That(closeResult.Tuple[1].String, Does.Contain("close:first"));
            });

            Table log = script.Call(script.Globals.Get("read_close_log")).Table;
            Assert.That(log.Length, Is.EqualTo(2));
            Assert.That(log.Get(1).String, Is.EqualTo("second:nil"));
            Assert.That(log.Get(2).String, Is.EqualTo("first:nil"));
        }
    }
}
