namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.CoreLib
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
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

        [global::TUnit.Core.Test]
        public async Task XpcallAllowsNilHandler()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                return xpcall(function() error('nil-handler') end, nil)
                "
            );

            await Assert.That(tuple.Tuple[0].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(tuple.Tuple[1].String).Contains("nil-handler").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task XpcallRejectsNonFunctionHandler()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return xpcall(function() end, 123)")
            )!;

            await Assert
                .That(exception.Message)
                .Contains("bad argument #2 to 'xpcall'")
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }
    }
}
