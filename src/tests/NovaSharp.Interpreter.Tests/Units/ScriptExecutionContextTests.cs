namespace NovaSharp.Interpreter.Tests.Units
{
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
                        Assert.That(tail.TailCallData.Args[1].Number, Is.EqualTo(7));
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
                    DynValue handler = context.GetScript().Globals.Get("decorator");

                    context.PerformMessageDecorationBeforeUnwind(handler, exception);

                    Assert.That(exception.DecoratedMessage, Is.EqualTo("decorated:boom"));
                    return DynValue.NewString(exception.DecoratedMessage);
                }
            );

            script.Globals["decorateMessage"] = callback;

            DynValue result = script.DoString("return decorateMessage()");
            Assert.That(result.String, Is.EqualTo("decorated:boom"));
        }
    }
}
