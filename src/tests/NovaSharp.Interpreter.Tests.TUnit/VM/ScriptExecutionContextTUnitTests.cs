namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Execution;
    using NovaSharp.Interpreter.Modules;

    public sealed class ScriptExecutionContextTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task EvaluateSymbolByNameResolvesLocals()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback(
                (context, _) =>
                {
                    DynValue local = context.EvaluateSymbolByName("localValue");
                    return local ?? DynValue.Nil;
                }
            );
            script.Globals["assertLocal"] = callback;

            DynValue result = script.DoString(
                @"
                function wrapper()
                    local localValue = 123
                    return assertLocal()
                end
                return wrapper()
            "
            );

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task CurrentGlobalEnvExposesGlobals()
        {
            Script script = new(CoreModules.PresetComplete);
            script.Globals["marker"] = DynValue.NewString("available");

            DynValue callback = DynValue.NewCallback(
                (context, _) =>
                {
                    Table env = context.CurrentGlobalEnv;
                    DynValue marker = env.Get("marker");
                    SymbolRef envSymbol = context.FindSymbolByName(WellKnownSymbols.ENV);
                    DynValue envValue = context.EvaluateSymbol(envSymbol);
                    return DynValue.NewTuple(marker, envValue);
                }
            );
            script.Globals["probeEnv"] = callback;

            DynValue tuple = script.DoString(
                @"
                function trigger()
                    return probeEnv()
                end
                return trigger()
            "
            );

            await Assert.That(tuple.Tuple.Length).IsEqualTo(2);
            await Assert.That(tuple.Tuple[0].String).IsEqualTo("available");
            await Assert.That(tuple.Tuple[1].Type).IsEqualTo(DataType.Table);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableReturnsAssignedMetatable()
        {
            Script script = new(CoreModules.PresetComplete);

            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    Table meta = context.GetMetatable(args[0]);
                    return meta?.Get("marker") ?? DynValue.Nil;
                }
            );
            script.Globals["probeMeta"] = callback;

            DynValue marker = script.DoString(
                @"
                local t = {}
                setmetatable(t, { marker = 42 })
                return probeMeta(t)
            "
            );

            await Assert.That(marker.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodTailCallReturnsTailCallRequest()
        {
            Script script = new(CoreModules.PresetComplete);
            LastTailCall = null;
            DynValue callback = DynValue.NewCallback(
                (context, args) =>
                {
                    LastTailCall = context.GetMetamethodTailCall(
                        args[0],
                        "__call",
                        args[0],
                        args[1]
                    );
                    return DynValue.NewNumber(0);
                }
            );
            script.Globals["probeTailCall"] = callback;

            script.DoString(
                @"
                local target = {}
                setmetatable(target, { __call = function(_, value) return value end })
                return probeTailCall(target, 7)
            "
            );

            DynValue tail = LastTailCall;
            await Assert.That(tail).IsNotNull();
            await Assert.That(tail.Type).IsEqualTo(DataType.TailCallRequest);
            await Assert.That(tail.TailCallData.Function.Type).IsEqualTo(DataType.Function);
            await Assert.That(tail.TailCallData.Args.Span[1].Number).IsEqualTo(7);
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationDecoratesException()
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
                (context, _) =>
                {
                    ScriptRuntimeException exception = new("boom");
                    DynValue handler = context.Script.Globals.Get("decorator");
                    context.PerformMessageDecorationBeforeUnwind(handler, exception);
                    return DynValue.NewString(exception.DecoratedMessage);
                }
            );
            script.Globals["decorateMessage"] = callback;

            DynValue result = script.DoString("return decorateMessage()");
            await Assert.That(result.String).IsEqualTo("decorated:boom");
        }

        [global::TUnit.Core.Test]
        public async Task AdditionalDataRequiresCallback()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            InvalidOperationException exception = ExpectException<InvalidOperationException>(() =>
                context.AdditionalData = "payload"
            );

            await Assert.That(exception.Message).Contains("no callback");
        }

        [global::TUnit.Core.Test]
        public async Task AdditionalDataFlowsThroughCallback()
        {
            Script script = new(default(CoreModules));
            CallbackFunction callback = new((_, _) => DynValue.Nil);
            ScriptExecutionContext context = script.CreateDynamicExecutionContext(callback);

            context.AdditionalData = 123;

            await Assert.That(context.AdditionalData).IsEqualTo(123);
            await Assert.That(callback.AdditionalData).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsWhenClrFunctionYields()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue func = DynValue.NewCallback(
                (_, _) => DynValue.NewYieldReq(Array.Empty<DynValue>())
            );

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func)
            );

            await Assert.That(exception.Message).Contains("yield across a CLR-call boundary");
        }

        [global::TUnit.Core.Test]
        public async Task CallThrowsWhenTailCallHasContinuation()
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

            ScriptRuntimeException exception = ExpectException<ScriptRuntimeException>(() =>
                context.Call(func)
            );
            await Assert.That(exception.Message).Contains("cannot be called directly");
        }

        [global::TUnit.Core.Test]
        public async Task CallFollowsTailCallWithoutContinuation()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue inner = DynValue.NewCallback(
                (_, args) => DynValue.NewNumber(args[0].Number + 24)
            );
            DynValue func = DynValue.NewCallback(
                (_, _) =>
                    DynValue.NewTailCallReq(
                        new TailCallData
                        {
                            Function = inner,
                            Args = new[] { DynValue.NewNumber(18) },
                        }
                    )
            );

            DynValue result = context.Call(func);
            await Assert.That(result.Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task CallUsesCallMetamethod()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            Table target = new(script);
            Table meta = new(script);
            meta.Set("__call", DynValue.NewCallback((_, _) => DynValue.NewString("called")));
            target.MetaTable = meta;

            DynValue result = context.Call(DynValue.NewTable(target), DynValue.NewNumber(1));
            await Assert.That(result.String).IsEqualTo("called");
        }

        [global::TUnit.Core.Test]
        public async Task EvaluateSymbolReturnsNilWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            DynValue nil = context.EvaluateSymbol(null);
            await Assert.That(nil).IsEqualTo(DynValue.Nil);
        }

        [global::TUnit.Core.Test]
        public async Task GetMetatableThrowsWhenValueIsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                context.GetMetatable(null)
            );
            await Assert.That(exception.ParamName).IsEqualTo("value");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodThrowsWhenArgumentsNull()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue value = DynValue.NewNumber(1);

            ArgumentNullException valueException = ExpectException<ArgumentNullException>(() =>
                context.GetMetamethod(null, "__call")
            );
            ArgumentNullException methodException = ExpectException<ArgumentNullException>(() =>
                context.GetMetamethod(value, null)
            );

            await Assert.That(valueException.ParamName).IsEqualTo("value");
            await Assert.That(methodException.ParamName).IsEqualTo("metamethod");
        }

        [global::TUnit.Core.Test]
        public async Task GetBinaryMetamethodValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue operand = DynValue.NewNumber(1);

            ArgumentNullException op1Exception = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(null, operand, "__add")
            );
            ArgumentNullException op2Exception = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(operand, null, "__add")
            );
            ArgumentNullException eventException = ExpectException<ArgumentNullException>(() =>
                context.GetBinaryMetamethod(operand, operand, null)
            );

            await Assert.That(op1Exception.ParamName).IsEqualTo("op1");
            await Assert.That(op2Exception.ParamName).IsEqualTo("op2");
            await Assert.That(eventException.ParamName).IsEqualTo("eventName");
        }

        [global::TUnit.Core.Test]
        public async Task EmulateClassicCallValidatesArguments()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            CallbackArguments args = new(Array.Empty<DynValue>(), false);

            ArgumentNullException argsException = ExpectException<ArgumentNullException>(() =>
                context.EmulateClassicCall(null, "fn", _ => 0)
            );
            ArgumentNullException callbackException = ExpectException<ArgumentNullException>(() =>
                context.EmulateClassicCall(args, "fn", null)
            );

            await Assert.That(argsException.ParamName).IsEqualTo("args");
            await Assert.That(callbackException.ParamName).IsEqualTo("callback");
        }

        [global::TUnit.Core.Test]
        public async Task CallValidatesFunctionArgument()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();

            ArgumentNullException exception = ExpectException<ArgumentNullException>(() =>
                context.Call(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("func");
        }

        [global::TUnit.Core.Test]
        public async Task GetMetamethodTailCallReturnsNullWhenMissing()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            DynValue target = DynValue.NewTable(new Table(script));

            DynValue tail = context.GetMetamethodTailCall(target, "__call");
            await Assert.That(tail).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task PerformMessageDecorationDefaultsToOriginal()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            ScriptRuntimeException exception = new("boom");

            context.PerformMessageDecorationBeforeUnwind(null, exception);
            await Assert.That(exception.DecoratedMessage).IsEqualTo("boom");
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsFalseForDynamicContexts()
        {
            Script script = new(default(CoreModules));
            ScriptExecutionContext context = script.CreateDynamicExecutionContext();
            await Assert.That(context.IsYieldable()).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsFalseForMainProcessor()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;

            DynValue result = script.DoString("return yieldState()");
            await Assert.That(result.Boolean).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task IsYieldableReturnsTrueInsideCoroutine()
        {
            Script script = new(CoreModules.PresetComplete);
            DynValue callback = DynValue.NewCallback(
                (context, _) => DynValue.NewBoolean(context.IsYieldable())
            );
            script.Globals["yieldState"] = callback;
            script.DoString("function coroutineProbe() return yieldState() end");

            DynValue coroutineHandle = script.CreateCoroutine(script.Globals.Get("coroutineProbe"));
            DynValue resumeResult = coroutineHandle.Coroutine.Resume();

            await Assert.That(resumeResult.Boolean).IsTrue();
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

        private static DynValue LastTailCall;
    }
}
