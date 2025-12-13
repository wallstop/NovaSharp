namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class ErrorHandlingModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PcallReturnsAllValuesOnSuccess()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local ok, a, b = pcall(function() return 1, 2 end)
                return ok, a, b
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(2d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallCapturesScriptErrors()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function() error('boom') end)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("boom").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallRejectsNonFunctions()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString("return pcall(123)");

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tuple.Tuple[1].String)
                .Contains("attempt to pcall a non-function")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallHandlesClrFunctionSuccess()
        {
            Script script = CreateScript();
            script.Globals["clr"] = DynValue.NewCallback(
                (context, args) =>
                    DynValue.NewTuple(DynValue.NewString("hello"), DynValue.NewNumber(5)),
                "clr"
            );

            DynValue tuple = script.DoString("return pcall(clr)");

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("hello").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(5d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallForwardsArgumentsToScriptFunction()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local function sum(a, b, c)
                    return a + b + c
                end

                local ok, value = pcall(sum, 2, 4, 8)
                return ok, value
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].Number).IsEqualTo(14d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallDecoratesClrScriptRuntimeExceptions()
        {
            Script script = CreateScript();
            script.Globals["clr"] = DynValue.NewCallback(
                (context, args) => throw new ScriptRuntimeException("fail"),
                "clr-fail"
            );

            DynValue tuple = script.DoString("return pcall(clr)");

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("fail").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallWrapsClrTailCallRequestWithoutHandlers()
        {
            Script script = CreateScript();
            script.Globals["tailing"] = DynValue.NewCallback(
                (context, args) =>
                    DynValue.NewTailCallReq(
                        DynValue.NewCallback((ctx, innerArgs) => DynValue.NewNumber(77), "inner")
                    ),
                "tailing-clr"
            );

            DynValue record = script.DoString(
                @"
                local ok, value = pcall(tailing)
                return { ok = ok, value = value, valueType = type(value) }
                "
            );

            await Assert.That(record.Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
            Table table = record.Table;

            DynValue ok = table.Get("ok");
            DynValue valueType = table.Get("valueType");
            DynValue value = table.Get("value");

            await Assert.That(ok.Boolean).IsTrue().ConfigureAwait(false);
            await Assert
                .That(valueType.String == "number" || valueType.String == "nil")
                .IsTrue()
                .ConfigureAwait(false);

            if (valueType.String == "number")
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
                await Assert.That(value.Number).IsEqualTo(77d).ConfigureAwait(false);
            }
            else
            {
                await Assert.That(value.IsNil()).IsTrue().ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task PcallRejectsClrTailCallWithContinuation()
        {
            Script script = CreateScript();
            script.Globals["tailing"] = DynValue.NewCallback(
                (context, args) =>
                {
                    TailCallData tailCall = new()
                    {
                        Function = DynValue.NewCallback((ctx, innerArgs) => DynValue.True, "inner"),
                        Args = System.Array.Empty<DynValue>(),
                        Continuation = new CallbackFunction(
                            (ctx, continuationArgs) => DynValue.True,
                            "continuation"
                        ),
                    };

                    return DynValue.NewTailCallReq(tailCall);
                },
                "tailing-clr"
            );

            DynValue tuple = script.DoString("return pcall(tailing)");

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tuple.Tuple[1].String)
                .Contains("wrap in a script function instead")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task PcallRejectsClrYieldRequest()
        {
            Script script = CreateScript();
            script.Globals["yielding"] = DynValue.NewCallback(
                (context, args) => DynValue.NewYieldReq(System.Array.Empty<DynValue>()),
                "yielding-clr"
            );

            DynValue tuple = script.DoString("return pcall(yielding)");

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(tuple.Tuple[1].String)
                .Contains("wrap in a script function instead")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task XpcallDecoratesClrExceptionWithHandlerBeforeUnwind()
        {
            Script script = CreateScript();
            script.Globals["clr"] = DynValue.NewCallback(
                (context, args) => throw new ScriptRuntimeException("failure"),
                "clr-fail"
            );

            script.DoString(
                @"
                function decorator(message)
                    return 'decorated:' .. message
                end
                "
            );

            DynValue tuple = script.DoString("return xpcall(clr, decorator)");

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("decorated:").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("failure").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task XpcallInvokesHandlerOnError()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local function handler(msg) return 'handled:' .. msg end
                local ok, err = xpcall(function() error('bad') end, handler)
                return ok, err
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("handled:").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("bad").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task XpcallReturnsSuccessWhenFunctionSucceeds()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local function succeed()
                    return 'done', 42
                end

                local function handler(msg)
                    return 'handled:' .. msg
                end

                return xpcall(succeed, handler)
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).IsEqualTo("done").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[2].Number).IsEqualTo(42d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task XpcallAcceptsClrHandler()
        {
            Script script = CreateScript();
            script.Globals["clrhandler"] = DynValue.NewCallback(
                (context, args) => DynValue.NewString("handled:" + args[0].String),
                "handler"
            );

            DynValue tuple = script.DoString(
                @"
                local function fail()
                    error('boom')
                end

                return xpcall(fail, clrhandler)
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("handled:").ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("boom").ConfigureAwait(false);
        }

        // Lua 5.1/5.2 allow nil handlers - when an error occurs and handler is nil,
        // the result is "error in error handling" per Lua spec
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallAllowsNilHandlerInLua51And52(LuaCompatibilityVersion version)
        {
            Script script = CreateScriptWithVersion(version);

            DynValue tuple = script.DoString(
                @"
                return xpcall(function() error('test error') end, nil)
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            // Per Lua spec: when handler fails (including nil), result is "error in error handling"
            await Assert
                .That(tuple.Tuple[1].String)
                .IsEqualTo("error in error handling")
                .ConfigureAwait(false);
        }

        // Lua 5.3+ validates the handler type upfront, including nil
        [global::TUnit.Core.Test]
        public async Task XpcallRejectsNilHandlerInLua53Plus()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua53);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return xpcall(function() end, nil)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("bad argument #2 to 'xpcall'")
                .ConfigureAwait(false);
        }

        // Lua 5.3+ validates the handler type upfront
        [global::TUnit.Core.Test]
        public async Task XpcallRejectsNonFunctionHandlerInLua53Plus()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua53);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return xpcall(function() end, 123)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("bad argument #2 to 'xpcall'")
                .ConfigureAwait(false);
        }

        // Lua 5.1 and 5.2 do NOT validate handler type upfront - they allow non-function handlers
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallAllowsNonFunctionHandlerInLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            // In Lua 5.1/5.2, xpcall with a non-function handler should return true
            // if the main function doesn't error (the handler is never invoked)
            DynValue result = script.DoString("return xpcall(function() end, 123)");

            await Assert.That(result.Boolean).IsTrue().ConfigureAwait(false);
        }

        // In Lua 5.1/5.2, if an error occurs and the handler is not callable,
        // the error message becomes "error in error handling"
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallReturnsErrorInErrorHandlingWhenHandlerNotCallableLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            // When main function errors and handler isn't callable, returns "error in error handling"
            DynValue result = script.DoString("return xpcall(function() error('test') end, 123)");

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("error in error handling")
                .ConfigureAwait(false);
        }

        // In Lua 5.1/5.2, xpcall with string handler also produces "error in error handling" when called
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallStringHandlerProducesErrorInErrorHandlingLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                "return xpcall(function() error('test') end, 'not-a-function')"
            );

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("error in error handling")
                .ConfigureAwait(false);
        }

        // In Lua 5.1/5.2, xpcall with table handler (no __call) produces "error in error handling"
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task XpcallTableHandlerWithoutCallProducesErrorInErrorHandlingLua51And52(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString("return xpcall(function() error('test') end, {})");

            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .Contains("error in error handling")
                .ConfigureAwait(false);
        }

        // Test all Lua 5.3+ versions reject non-function handlers
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task XpcallRejectsNonFunctionHandlerInAllLua53PlusVersions(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return xpcall(function() end, 123)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("bad argument #2 to 'xpcall'")
                .ConfigureAwait(false);
        }

        // Test all Lua 5.3+ versions reject nil handlers
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task XpcallRejectsNilHandlerInAllLua53PlusVersions(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return xpcall(function() end, nil)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("bad argument #2 to 'xpcall'")
                .ConfigureAwait(false);
        }

        // =====================================================
        // xpcall Extra Arguments Version Parity Tests
        // =====================================================
        // Lua 5.1: xpcall(f, err) — Only 2 arguments, extra args are IGNORED
        // Lua 5.2+: xpcall(f, msgh [,arg1, ...]) — Extra args passed to f

        /// <summary>
        /// Verifies that Lua 5.1 mode ignores extra arguments passed to xpcall.
        /// Per the Lua 5.1 spec, xpcall only accepts 2 arguments (function and error handler).
        /// NOTE: There is a pre-existing bug in NovaSharp where pcall/xpcall pass an extra nil
        /// argument when called with no extra args. This test focuses on the 5.1 vs 5.2+ difference:
        /// In 5.1, extra args should be ignored; in 5.2+, they should be passed.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task XpcallIgnoresExtraArgumentsInLua51()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua51);

            // In Lua 5.1, extra args (1, 2, 3) should NOT be passed to the function
            // The function should receive the same args as if called without extras
            DynValue result = script.DoString(
                @"
                local receivedWithExtras = {}
                local receivedWithoutExtras = {}
                
                -- Call with extra args
                xpcall(function(...) 
                    for i, v in ipairs({...}) do receivedWithExtras[i] = v end
                end, function() end, 1, 2, 3)
                
                -- Call without extra args
                xpcall(function(...) 
                    for i, v in ipairs({...}) do receivedWithoutExtras[i] = v end
                end, function() end)
                
                -- In Lua 5.1, both should receive the same number of args
                return #receivedWithExtras, #receivedWithoutExtras
                "
            );

            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2).ConfigureAwait(false);
            // The key assertion: both should have the same count (5.1 ignores extra args)
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(result.Tuple[1].Number)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.2 mode passes extra arguments to the function in xpcall.
        /// This feature was added in Lua 5.2.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task XpcallPassesExtraArgumentsInLua52()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua52);

            DynValue result = script.DoString(
                @"
                local received = {}
                local ok, a, b, c = xpcall(function(...) 
                    for i, v in ipairs({...}) do received[i] = v end
                    return ...
                end, function() end, 1, 2, 3)
                return ok, #received, a, b, c
                "
            );

            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(5).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            // In Lua 5.2+, the function receives the extra args
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[4].Number).IsEqualTo(3).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.3 mode passes extra arguments to the function in xpcall.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task XpcallPassesExtraArgumentsInLua53()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua53);

            DynValue result = script.DoString(
                @"
                local ok, a, b, c = xpcall(function(...) 
                    return ...
                end, function() end, 10, 20, 30)
                return ok, a, b, c
                "
            );

            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(10).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(20).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(30).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.4 mode passes extra arguments to the function in xpcall.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task XpcallPassesExtraArgumentsInLua54()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua54);

            DynValue result = script.DoString(
                @"
                local ok, a, b, c = xpcall(function(...) 
                    return ...
                end, function() end, 100, 200, 300)
                return ok, a, b, c
                "
            );

            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(100).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(200).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(300).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that xpcall extra arguments work correctly when the function errors in Lua 5.2+.
        /// The extra arguments should still be available to the function before the error.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        public async Task XpcallExtraArgsAvailableBeforeErrorInLua52Plus(
            LuaCompatibilityVersion version
        )
        {
            Script script = CreateScriptWithVersion(version);

            DynValue result = script.DoString(
                @"
                local captured = nil
                local ok, err = xpcall(function(a, b, c) 
                    captured = a + b + c
                    error('intentional error')
                end, function(e) return 'handled: ' .. e end, 10, 20, 30)
                return ok, captured
                "
            );

            await Assert.That(result.Tuple.Length).IsGreaterThanOrEqualTo(2).ConfigureAwait(false);
            await Assert.That(result.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            // The function should have received and processed the arguments before erroring
            await Assert.That(result.Tuple[1].Number).IsEqualTo(60).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Lua 5.1 mode ignores extra arguments even when many are passed.
        /// This confirms the function receives the same args whether or not extras are provided.
        /// </summary>
        [global::TUnit.Core.Test]
        public async Task XpcallIgnoresAllExtraArgumentsInLua51()
        {
            Script script = CreateScriptWithVersion(LuaCompatibilityVersion.Lua51);

            DynValue result = script.DoString(
                @"
                local countWithExtras = 0
                local countWithoutExtras = 0
                
                -- Call with many extra args
                xpcall(function(...) 
                    countWithExtras = select('#', ...)
                end, function() end, 'a', 'b', 'c', 'd', 'e', 1, 2, 3, 4, 5)
                
                -- Call without extra args  
                xpcall(function(...) 
                    countWithoutExtras = select('#', ...)
                end, function() end)
                
                -- In Lua 5.1, both should receive the same number of args
                return countWithExtras, countWithoutExtras
                "
            );

            // The key assertion: both should have the same count (5.1 ignores extra args)
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(result.Tuple[1].Number)
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            Script script = new(CoreModulePresets.Complete);
            script.Options.DebugPrint = _ => { };
            return script;
        }

        private static Script CreateScriptWithVersion(LuaCompatibilityVersion version)
        {
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = version,
            };
            Script script = new(CoreModulePresets.Complete, options);
            script.Options.DebugPrint = _ => { };
            return script;
        }
    }
}
