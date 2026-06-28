namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ProcessorExecution
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ProcessorCoroutineModuleTUnitTests
    {
        /// <summary>
        /// Tests that coroutine.running() returns nil from main in Lua 5.1.
        /// In Lua 5.1, coroutine.running() returns nil when called from the main thread.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task RunningFromMainReturnsNilInLua51(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Nil)
                .Because(
                    $"Lua 5.1 coroutine.running() returns nil from main thread, got {result.Type}"
                );
        }

        /// <summary>
        /// Tests that coroutine.running() returns (thread, true) from main in Lua 5.2+.
        /// In Lua 5.2+, coroutine.running() returns the main coroutine and a boolean indicating it's the main thread.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RunningFromMainReturnsMainCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            DynValue result = script.Call(runningFunc);
            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Tuple)
                .Because($"Lua 5.2+ coroutine.running() returns a tuple, got {result.Type}");
            await Assert
                .That(result.Tuple.Length)
                .IsGreaterThanOrEqualTo(2)
                .Because($"Expected at least 2 tuple elements, got {result.Tuple.Length}");

            DynValue coroutineValue = result.Tuple[0];
            DynValue isMain = result.Tuple[1];

            await Assert.That(coroutineValue.Type).IsEqualTo(DataType.Thread);
            await Assert.That(coroutineValue.Coroutine.State).IsEqualTo(CoroutineState.Main);
            await Assert.That(isMain.Type).IsEqualTo(DataType.Boolean);
            await Assert.That(isMain.Boolean).IsTrue();
        }

        /// <summary>
        /// Tests that coroutine.running() inside a coroutine returns the coroutine in Lua 5.1.
        /// In Lua 5.1, coroutine.running() inside a coroutine returns only the coroutine (single value), no isMain boolean.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task RunningInsideCoroutineReturnsCoroutineInLua51(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function runningCheck()
                    local co = coroutine.running()
                    return type(co), co
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("runningCheck"));
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Tuple)
                .Because($"Expected tuple result, got {result.Type}");
            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("thread")
                .Because($"Expected 'thread' type, got {result.Tuple[0].String}");
            await Assert
                .That(result.Tuple[1].Type)
                .IsEqualTo(DataType.Thread)
                .Because(
                    $"Lua 5.1 coroutine.running() inside coroutine returns thread, got {result.Tuple[1].Type}"
                );
        }

        /// <summary>
        /// Tests that coroutine.running() inside a coroutine returns (thread, false) in Lua 5.2+.
        /// In Lua 5.2+, coroutine.running() returns a tuple of (coroutine, isMain) where isMain is false inside a coroutine.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RunningInsideCoroutineReturnsFalse(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Boolean)
                .Because($"Expected boolean result, got {result.Type}");
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StatusReflectsLifecycleAndForceSuspendedStates(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
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
            await Assert.That(initialStatus.String).IsEqualTo("suspended");

            coroutineValue.Coroutine.AutoYieldCounter = 1;
            DynValue forced = coroutineValue.Coroutine.Resume();
            await Assert.That(forced.Type).IsEqualTo(DataType.YieldRequest);
            await Assert.That(forced.YieldRequest.Forced).IsTrue();

            DynValue suspendedStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(suspendedStatus.String).IsEqualTo("suspended");

            coroutineValue.Coroutine.AutoYieldCounter = 0;
            DynValue final = coroutineValue.Coroutine.Resume();
            await Assert.That(final.Type).IsEqualTo(DataType.Number);

            DynValue deadStatus = script.Call(statusFunc, coroutineValue);
            await Assert.That(deadStatus.String).IsEqualTo("dead");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StatusReturnsRunningForActiveCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(
                script.Globals.Get("queryRunningStatus")
            );
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("running");
        }

        /// <summary>
        /// Tests that coroutine.status() on the main coroutine returns 'normal' when called from a child.
        /// Note: This test requires Lua 5.2+ because coroutine.running() returns nil from main in Lua 5.1.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StatusReturnsNormalWhenInspectingMainFromChild(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local mainCoroutine = select(1, coroutine.running())

                function queryMainStatus()
                    return coroutine.status(mainCoroutine)
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("queryMainStatus"));
            DynValue result = coroutineValue.Coroutine.Resume();

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("normal");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task StatusThrowsForUnknownStates(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function idle()
                    return 1
                end
            "
            );

            DynValue statusFunc = script.Globals.Get("coroutine").Table.Get("status");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("idle"));

            coroutineValue.Coroutine.ForceStateForTests((CoroutineState)0);

            InternalErrorException exception = ExpectException<InternalErrorException>(() =>
                script.Call(statusFunc, coroutineValue)
            );
            await Assert.That(exception.Message).Contains("Unexpected coroutine state");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WrapRequiresFunctionArgument(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue wrapFunc = script.Globals.Get("coroutine").Table.Get("wrap");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(wrapFunc, DynValue.NewNumber(1))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'wrap'");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task CreateRequiresFunctionArgument(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue createFunc = script.Globals.Get("coroutine").Table.Get("create");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(createFunc, DynValue.NewString("oops"))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'create'");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task WrapReturnsFunctionThatResumesCoroutine(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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
            await Assert.That(wrapper.Type).IsEqualTo(DataType.ClrFunction);

            DynValue first = script.Call(wrapper);
            DynValue second = script.Call(wrapper);
            DynValue third = script.Call(wrapper);
            DynValue final = script.Call(wrapper);

            await Assert.That(first.Number).IsEqualTo(1d);
            await Assert.That(second.Number).IsEqualTo(2d);
            await Assert.That(third.Number).IsEqualTo(3d);
            await Assert.That(final.String).IsEqualTo("done");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeFlattensResultsAndReportsSuccess(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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
            await Assert.That(first.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(first.Tuple.Length).IsEqualTo(3);
            await Assert.That(first.Tuple[0].Boolean).IsTrue();
            await Assert.That(first.Tuple[1].String).IsEqualTo("yielded");
            await Assert.That(first.Tuple[2].Number).IsEqualTo(42d);

            DynValue second = script.Call(resumeFunc, coroutineValue);
            await Assert.That(second.Tuple[0].Boolean).IsTrue();
            await Assert.That(second.Tuple[1].Number).IsEqualTo(7d);
            await Assert.That(second.Tuple[2].Number).IsEqualTo(8d);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeReportsErrorsAsFalseWithMessage(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
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
            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple[0].Boolean).IsFalse();
            await Assert.That(result.Tuple[1].String).Contains("boom");
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeRequiresThreadArgument(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                script.Call(resumeFunc, DynValue.NewString("oops"))
            );
            await Assert.That(exception.Message).Contains("bad argument #1 to 'resume'");
        }

        /// <summary>
        /// Tests that resume flattens nested tuple results in Lua 5.1.
        /// In Lua 5.1, coroutine.running() returns only the coroutine (1 value), so the tuple has 3 elements.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task ResumeFlattensNestedTupleResultsLua51(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function returningTuple()
                    return 'tag', coroutine.running()
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("returningTuple"));

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Tuple)
                .Because($"Expected tuple result, got {result.Type}");
            // In Lua 5.1: resume returns (true, 'tag', coroutine) = 3 elements
            await Assert
                .That(result.Tuple.Length)
                .IsEqualTo(3)
                .Because(
                    $"Lua 5.1: resume(co) where co returns ('tag', coroutine.running()) should be (true, 'tag', thread) = 3 elements, got {result.Tuple.Length}"
                );
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("tag");
            await Assert.That(result.Tuple[2].Type).IsEqualTo(DataType.Thread);
        }

        /// <summary>
        /// Tests that resume flattens nested tuple results in Lua 5.2+.
        /// In Lua 5.2+, coroutine.running() returns (thread, isMain), so the tuple has 4 elements.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeFlattensNestedTupleResults(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function returningTuple()
                    return 'tag', coroutine.running()
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("returningTuple"));

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.Tuple)
                .Because($"Expected tuple result, got {result.Type}");
            // In Lua 5.2+: resume returns (true, 'tag', coroutine, isMain) = 4 elements
            await Assert
                .That(result.Tuple.Length)
                .IsEqualTo(4)
                .Because(
                    $"Lua 5.2+: resume(co) where co returns ('tag', coroutine.running()) should be (true, 'tag', thread, false) = 4 elements, got {result.Tuple.Length}"
                );
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("tag");
            await Assert.That(result.Tuple[2].Type).IsEqualTo(DataType.Thread);
            await Assert.That(result.Tuple[3].Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task ResumeDeeplyNestedTuplesAreFlattened(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                function buildDeepCoroutine()
                    local function deepest()
                        return 'deep', 'value'
                    end

                    local function middle()
                        return coroutine.resume(coroutine.create(deepest))
                    end

                    local function top()
                        return 'top', coroutine.resume(coroutine.create(middle))
                    end

                    return coroutine.create(top)
                end
            "
            );

            DynValue resumeFunc = script.Globals.Get("coroutine").Table.Get("resume");
            DynValue coroutineValue = script.Call(script.Globals.Get("buildDeepCoroutine"));

            DynValue result = script.Call(resumeFunc, coroutineValue);

            await Assert.That(result.Type).IsEqualTo(DataType.Tuple);
            await Assert.That(result.Tuple.Length).IsEqualTo(6);
            await Assert.That(result.Tuple[0].Boolean).IsTrue();
            await Assert.That(result.Tuple[1].String).IsEqualTo("top");
            await Assert.That(result.Tuple[2].Boolean).IsTrue();
            await Assert.That(result.Tuple[3].Boolean).IsTrue();
            await Assert.That(result.Tuple[4].String).IsEqualTo("deep");
            await Assert.That(result.Tuple[5].String).IsEqualTo("value");
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

        // =====================================================
        // Edge Case Tests: coroutine.running() Version Behavior
        // =====================================================

        /// <summary>
        /// Tests that coroutine.running() returns consistent values across nested coroutine calls in Lua 5.2+.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RunningReturnsConsistentValuesInNestedCoroutines(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                results = {}
                function outer()
                    local co, isMain = coroutine.running()
                    results.outer = { co = co, isMain = isMain }
                    
                    local inner = coroutine.create(function()
                        local co2, isMain2 = coroutine.running()
                        results.inner = { co = co2, isMain = isMain2 }
                    end)
                    
                    coroutine.resume(inner)
                    return results
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("outer"));
            coroutineValue.Coroutine.Resume();

            Table results = script.Globals.Get("results").Table;
            Table outer = results.Get("outer").Table;
            Table inner = results.Get("inner").Table;

            // Both should return isMain=false when inside a coroutine
            await Assert
                .That(outer.Get("isMain").Boolean)
                .IsFalse()
                .Because("outer coroutine should have isMain=false");
            await Assert
                .That(inner.Get("isMain").Boolean)
                .IsFalse()
                .Because("inner coroutine should have isMain=false");

            // The coroutines should be different
            await Assert
                .That(outer.Get("co").Coroutine)
                .IsNotEqualTo(inner.Get("co").Coroutine)
                .Because("nested coroutines should have different thread references");
        }

        /// <summary>
        /// Tests that Lua 5.1 behavior for coroutine.running() is correct in nested calls.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task RunningInNestedCoroutinesLua51(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                results = {}
                function outer()
                    results.outer = coroutine.running()
                    
                    local inner = coroutine.create(function()
                        results.inner = coroutine.running()
                    end)
                    
                    coroutine.resume(inner)
                end
            "
            );

            DynValue coroutineValue = script.CreateCoroutine(script.Globals.Get("outer"));
            coroutineValue.Coroutine.Resume();

            Table results = script.Globals.Get("results").Table;

            // In Lua 5.1, coroutine.running() returns only the thread, not a tuple
            await Assert
                .That(results.Get("outer").Type)
                .IsEqualTo(DataType.Thread)
                .Because("Lua 5.1 coroutine.running() returns thread");
            await Assert
                .That(results.Get("inner").Type)
                .IsEqualTo(DataType.Thread)
                .Because("Lua 5.1 coroutine.running() returns thread in nested");

            // The coroutines should be different
            await Assert
                .That(results.Get("outer").Coroutine)
                .IsNotEqualTo(results.Get("inner").Coroutine)
                .Because("nested coroutines should have different thread references");
        }

        /// <summary>
        /// Tests that coroutine.running() returns the correct coroutine reference in a deeply nested call.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RunningReturnsSameCoroutineAsCreate(LuaCompatibilityVersion version)
        {
            Script script = new(version, CoreModulePresets.Complete);
            DynValue runningFunc = script.Globals.Get("coroutine").Table.Get("running");

            script.DoString(
                @"
                function getRunning()
                    local co = coroutine.running()
                    return co
                end
            "
            );

            DynValue created = script.CreateCoroutine(script.Globals.Get("getRunning"));
            DynValue result = created.Coroutine.Resume();

            // In both 5.1 and 5.2+, the coroutine reference should match
            await Assert
                .That(result.Coroutine)
                .IsEqualTo(created.Coroutine)
                .Because("coroutine.running() should return the same coroutine as create");
        }
    }
}
