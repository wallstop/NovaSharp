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
