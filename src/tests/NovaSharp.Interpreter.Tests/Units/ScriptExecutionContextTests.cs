namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptExecutionContextTests
    {
        [Test]
        public void EvaluateSymbolByNameResolvesLocals()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    DynValue local = context.EvaluateSymbolByName("localValue");
                    Assert.That(local, Is.Not.Null);
                    Assert.That(local.Type, Is.EqualTo(DataType.Number));
                    Assert.That(local.Number, Is.EqualTo(123));
                    return DynValue.NewNumber(0);
                }
            );

            script.Globals["assertLocal"] = callback;

            script.DoString(
                @"
                function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
            "
            );
        }

        [Test]
        public void ScriptExecutionContextExposesGlobalEnvironment()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Globals["marker"] = DynValue.NewString("available");

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    Table env = context.CurrentGlobalEnv;
                    Assert.That(env, Is.Not.Null);
                    Assert.That(env.Get("marker").String, Is.EqualTo("available"));

                    SymbolRef envSymbol = context.FindSymbolByName(WellKnownSymbols.ENV);
                    Assert.That(envSymbol, Is.Not.Null);
                    DynValue envValue = context.EvaluateSymbol(envSymbol);
                    Assert.That(envValue.Type, Is.EqualTo(DataType.Table));

                    return DynValue.NewNumber(0);
                }
            );

            script.Globals["probeEnv"] = callback;

            script.DoString(
                @"
                function trigger()
                    return probeEnv()
                end
                return trigger()
            "
            );
        }

        [Test]
        public void GetMetatableReturnsAssignedMetatable()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    DynValue target = args[0];
                    Table meta = context.GetMetatable(target);
                    Assert.That(meta, Is.Not.Null);
                    Assert.That(meta.Get("marker").Number, Is.EqualTo(42));
                    return DynValue.NewNumber(0);
                }
            );

            script.Globals["probeMeta"] = callback;

            script.DoString(
                @"
                local t = {}
                setmetatable(t, { marker = 42 })
                probeMeta(t)
            "
            );
        }

        [Test]
        public void GetMetamethodTailCallReturnsTailCallRequest()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    DynValue tail = context.GetMetamethodTailCall(
                        args[0],
                        "__call",
                        args[0],
                        args[1]
                    );

                    Assert.Multiple(() =>
                    {
                        Assert.That(tail.Type, Is.EqualTo(DataType.TailCallRequest));
                        Assert.That(tail.TailCallData.Function.Type, Is.EqualTo(DataType.Function));
                        Assert.That(tail.TailCallData.Args.Span[1].Number, Is.EqualTo(7));
                    });

                    return DynValue.NewNumber(0);
                }
            );

            script.Globals["probeTailCall"] = callback;

            script.DoString(
                @"
                local target = {}
                setmetatable(target, { __call = function(self, value) return value end })
                probeTailCall(target, 7)
            "
            );
        }

        [Test]
        public void PerformMessageDecorationBeforeUnwindDecoratesException()
        {
            Script script = new(CoreModules.PresetComplete);

            script.DoString(
                @"
                function decorator(message)
                    return 'decorated:' .. message
                end
            "
            );

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    ScriptRuntimeException exception = new("boom");
                    DynValue handler = context.Script.Globals.Get("decorator");

                    context.PerformMessageDecorationBeforeUnwind(handler, exception);

                    Assert.That(exception.DecoratedMessage, Is.EqualTo("decorated:boom"));
                    return DynValue.NewString(exception.DecoratedMessage);
                }
            );

            script.Globals["decorateMessage"] = callback;

            DynValue result = script.DoString("return decorateMessage()");
            Assert.That(result.String, Is.EqualTo("decorated:boom"));
        }

        [Test]
        public void AdditionalDataRequiresCallback()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            Assert.That(
                () => context.AdditionalData = "payload",
                Throws.TypeOf<InvalidOperationException>().With.Message.Contains("no callback")
            );
        }

        [Test]
        public void AdditionalDataFlowsThroughCallback()
        {
            Script script = new(default(CoreModules));
            CallbackFunction callback = new((_, _) => DynValue.Nil);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext(callback);

            context.AdditionalData = 123;

            Assert.Multiple(() =>
            {
                Assert.That(context.AdditionalData, Is.EqualTo(123));
                Assert.That(callback.AdditionalData, Is.EqualTo(123));
            });
        }

        [Test]
        public void CallThrowsWhenClrFunctionYields()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue func = DynValue.NewCallback(
                (_, _) => DynValue.NewYieldReq(Array.Empty<DynValue>())
            );

            Assert.That(
                () => context.Call(func),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("yield across a CLR-call boundary")
            );
        }

        [Test]
        public void CallThrowsWhenTailCallHasContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue func = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = DynValue.NewCallback((_, _) => DynValue.NewNumber(1)),
                            Continuation = new CallbackFunction((_, _) => DynValue.Nil),
                        }
                    )
            );

            Assert.That(
                () => context.Call(func),
                Throws
                    .TypeOf<ScriptRuntimeException>()
                    .With.Message.Contains("cannot be called directly")
            );
        }

        [Test]
        public void CallFollowsTailCallWithoutContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue inner = DynValue.NewCallback(
                (_, args) =>
                {
                    Assert.That(args.Count, Is.EqualTo(1));
                    Assert.That(args[0].Number, Is.EqualTo(99));
                    return DynValue.NewNumber(123);
                }
            );
            DynValue func = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = inner,
                            Args = new[] { DynValue.NewNumber(99) },
                        }
                    )
            );

            DynValue result = context.Call(func);
            Assert.That(result.Number, Is.EqualTo(123));
        }

        [Test]
        public void CallUsesCallMetamethod()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            Table target = new(script);
            Table meta = new(script);
            meta.Set("__call", DynValue.NewCallback((_, _) => DynValue.NewString("called")));
            target.MetaTable = meta;

            DynValue result = context.Call(DynValue.NewTable(target), DynValue.NewNumber(1));
            Assert.That(result.String, Is.EqualTo("called"));
        }

        [Test]
        public void EvaluateSymbolReturnsNilWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue nil = context.EvaluateSymbol(null);
            Assert.That(nil, Is.EqualTo(DynValue.Nil));
        }

        [Test]
        public void GetMetatableThrowsWhenValueIsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            Assert.That(
                () => context.GetMetatable(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("value")
            );
        }

        [Test]
        public void GetMetamethodThrowsWhenArgumentsAreNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue value = DynValue.NewNumber(1);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => context.GetMetamethod(null, "__call"),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("value")
                );
                Assert.That(
                    () => context.GetMetamethod(value, null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("metamethod")
                );
            });
        }

        [Test]
        public void GetBinaryMetamethodValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue operand = DynValue.NewNumber(1);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => context.GetBinaryMetamethod(null, operand, "__add"),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("op1")
                );
                Assert.That(
                    () => context.GetBinaryMetamethod(operand, null, "__add"),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("op2")
                );
                Assert.That(
                    () => context.GetBinaryMetamethod(operand, operand, null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("eventName")
                );
            });
        }

        [Test]
        public void EmulateClassicCallValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(Array.Empty<DynValue>(), isMethodCall: false);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => context.EmulateClassicCall(null, "fn", _ => 0),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("args")
                );
                Assert.That(
                    () => context.EmulateClassicCall(args, "fn", null),
                    Throws.ArgumentNullException.With.Property("ParamName").EqualTo("callback")
                );
            });
        }

        [Test]
        public void CallValidatesFunctionArgument()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            Assert.That(
                () => context.Call(null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("func")
            );
        }

        [Test]
        public void GetMetamethodTailCallReturnsNullWhenMetamethodMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue target = DynValue.NewTable(new Table(script));

            DynValue tail = context.GetMetamethodTailCall(target, "__call", DynValue.NewNumber(1));
            Assert.That(tail, Is.Null);
        }

        [Test]
        public void PerformMessageDecorationDefaultsToOriginalMessage()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            ScriptRuntimeException exception = new("boom");

            context.PerformMessageDecorationBeforeUnwind(null, exception);

            Assert.That(exception.DecoratedMessage, Is.EqualTo("boom"));
        }

        [Test]
        public void IsYieldableReturnsFalseForDynamicContexts()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Assert.That(context.IsYieldable(), Is.False);
        }

        [Test]
        public void IsYieldableReturnsFalseForMainProcessorCallbacks()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;

            DynValue result = script.DoString("return yieldState()");
            Assert.That(result.Boolean, Is.False);
        }

        [Test]
        public void IsYieldableReturnsTrueInsideCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;
            script.DoString("function coroutineProbe() return yieldState() end");

            DynValue coroutineHandle = script.CreateCoroutine(script.Globals.Get("coroutineProbe"));
            DynValue resumeResult = coroutineHandle.Coroutine.Resume();

            Assert.That(resumeResult.Boolean, Is.True);
        }
    }
}
