namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ErrorHandlingModuleTests
    {
        [Test]
        public void PcallReturnsAllValuesOnSuccess()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local ok, a, b = pcall(function() return 1, 2 end)
                return ok, a, b
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(1d));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(2d));
            });
        }

        [Test]
        public void PcallCapturesScriptErrors()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local ok, err = pcall(function() error('boom') end)
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("boom"));
            });
        }

        [Test]
        public void PcallRejectsNonFunctions()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString("return pcall(123)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("attempt to pcall a non-function"));
            });
        }

        [Test]
        public void PcallHandlesClrFunctionSuccess()
        {
            Script script = CreateScript();
            script.Globals["clr"] = DynValue.NewCallback(
                (context, args) =>
                    DynValue.NewTuple(DynValue.NewString("hello"), DynValue.NewNumber(5)),
                "clr"
            );

            DynValue tuple = script.DoString("return pcall(clr)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("hello"));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(5d));
            });
        }

        [Test]
        public void PcallForwardsArgumentsToScriptFunction()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].Number, Is.EqualTo(14d));
            });
        }

        [Test]
        public void PcallDecoratesClrScriptRuntimeExceptions()
        {
            Script script = CreateScript();
            script.Globals["clr"] = DynValue.NewCallback(
                (context, args) => throw new ScriptRuntimeException("fail"),
                "clr-fail"
            );

            DynValue tuple = script.DoString("return pcall(clr)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("fail"));
            });
        }

        [Test]
        public void PcallWrapsClrTailCallRequestWithoutHandlers()
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

            Assert.That(record.Type, Is.EqualTo(DataType.Table));
            Table table = record.Table;

            DynValue ok = table.Get("ok");
            DynValue valueType = table.Get("valueType");
            DynValue value = table.Get("value");

            Assert.Multiple(() =>
            {
                Assert.That(ok.Boolean, Is.True);
                Assert.That(valueType.String, Is.EqualTo("number").Or.EqualTo("nil"));

                if (valueType.String == "number")
                {
                    Assert.That(value.Type, Is.EqualTo(DataType.Number));
                    Assert.That(value.Number, Is.EqualTo(77d));
                }
                else
                {
                    Assert.That(value.IsNil(), Is.True);
                }
            });
        }

        [Test]
        public void PcallRejectsClrTailCallWithContinuation()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(
                    tuple.Tuple[1].String,
                    Does.Contain("wrap in a script function instead")
                );
            });
        }

        [Test]
        public void PcallRejectsClrYieldRequest()
        {
            Script script = CreateScript();
            script.Globals["yielding"] = DynValue.NewCallback(
                (context, args) => DynValue.NewYieldReq(System.Array.Empty<DynValue>()),
                "yielding-clr"
            );

            DynValue tuple = script.DoString("return pcall(yielding)");

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(
                    tuple.Tuple[1].String,
                    Does.Contain("wrap in a script function instead")
                );
            });
        }

        [Test]
        public void XpcallDecoratesClrExceptionWithHandlerBeforeUnwind()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("decorated:"));
                Assert.That(tuple.Tuple[1].String, Does.Contain("failure"));
            });
        }

        [Test]
        public void XpcallInvokesHandlerOnError()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                local function handler(msg) return 'handled:' .. msg end
                local ok, err = xpcall(function() error('bad') end, handler)
                return ok, err
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("handled:"));
                Assert.That(tuple.Tuple[1].String, Does.Contain("bad"));
            });
        }

        [Test]
        public void XpcallReturnsSuccessWhenFunctionSucceeds()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.True);
                Assert.That(tuple.Tuple[1].String, Is.EqualTo("done"));
                Assert.That(tuple.Tuple[2].Number, Is.EqualTo(42d));
            });
        }

        [Test]
        public void XpcallAcceptsClrHandler()
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

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("handled:"));
                Assert.That(tuple.Tuple[1].String, Does.Contain("boom"));
            });
        }

        [Test]
        public void XpcallAllowsNilHandler()
        {
            Script script = CreateScript();

            DynValue tuple = script.DoString(
                @"
                return xpcall(function() error('nil-handler') end, nil)
                "
            );

            Assert.Multiple(() =>
            {
                Assert.That(tuple.Tuple[0].Boolean, Is.False);
                Assert.That(tuple.Tuple[1].String, Does.Contain("nil-handler"));
            });
        }

        [Test]
        public void XpcallRejectsNonFunctionHandler()
        {
            Script script = CreateScript();

            Assert.That(
                () => script.DoString("return xpcall(function() end, 123)"),
                Throws
                    .InstanceOf<ScriptRuntimeException>()
                    .With.Message.Contains("bad argument #2 to 'xpcall'")
            );
        }

        private static Script CreateScript()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Options.DebugPrint = _ => { };
            return script;
        }
    }
}
