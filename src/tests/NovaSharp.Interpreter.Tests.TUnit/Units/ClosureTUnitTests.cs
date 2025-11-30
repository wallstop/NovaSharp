#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class ClosureTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetUpValuesTypeReturnsEnvironmentWhenOnlyEnvIsCaptured()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a) return a end");

            Closure closure = function.Function;

            await Assert.That(closure.UpValuesCount).IsEqualTo(1);
            await Assert.That(closure.GetUpValueName(0)).IsEqualTo(WellKnownSymbols.ENV);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(Closure.UpValuesType.Environment);
        }

        [global::TUnit.Core.Test]
        public async Task MetadataPropertiesExposeScriptAndEntryPoint()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return 42 end");
            Closure closure = function.Function;

            await Assert.That(closure.OwnerScript).IsSameReferenceAs(script);
            await Assert.That(closure.EntryPointByteCodeLocation).IsGreaterThanOrEqualTo(0);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValuesTypeDetectsEnvironmentUpValue()
        {
            Script script = new();
            DynValue function = script.DoString("return function() return _ENV end");

            Closure closure = function.Function;

            await Assert.That(closure.UpValuesCount).IsEqualTo(1);
            await Assert.That(closure.GetUpValueName(0)).IsEqualTo(WellKnownSymbols.ENV);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(Closure.UpValuesType.Environment);
        }

        [global::TUnit.Core.Test]
        public async Task GetUpValuesExposesCapturedSymbols()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                local x = 3
                local y = 4
                return function()
                    return x + y
                end
                "
            );

            Closure closure = function.Function;

            int upvalueCount = closure.UpValuesCount;
            List<string> names = new(upvalueCount);
            for (int i = 0; i < upvalueCount; i++)
            {
                names.Add(closure.GetUpValueName(i));
            }

            int envIndex = names.IndexOf(WellKnownSymbols.ENV);
            int xIndex = names.IndexOf("x");
            int yIndex = names.IndexOf("y");

            await Assert.That(upvalueCount).IsEqualTo(3);
            await Assert.That(envIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(xIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(yIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(closure.GetUpValue(xIndex).Number).IsEqualTo(3d);
            await Assert.That(closure.GetUpValue(yIndex).Number).IsEqualTo(4d);
            await Assert.That(closure.CapturedUpValuesType).IsEqualTo(Closure.UpValuesType.Closure);
        }

        [global::TUnit.Core.Test]
        public async Task DelegatesInvokeScriptFunction()
        {
            Script script = new();
            DynValue function = script.DoString("return function(a, b) return a + b end");
            Closure closure = function.Function;

            ScriptFunctionCallback generic = closure.GetDelegate();
            object genericResult = generic(1, 2);

            ScriptFunctionCallback<int> typed = closure.GetDelegate<int>();
            int typedResult = typed(5, 7);

            await Assert.That(genericResult).IsEqualTo(3d);
            await Assert.That(typedResult).IsEqualTo(12);
        }

        [global::TUnit.Core.Test]
        public async Task CallOverloadsInvokeUnderlyingFunction()
        {
            Script script = new();
            DynValue function = script.DoString(
                "return function(a, b) return (a or 0) + (b or 0) end"
            );
            Closure closure = function.Function;

            DynValue noArgs = closure.Call();
            DynValue objectArgs = closure.Call(2, 3);
            DynValue dynValues = closure.Call(DynValue.NewNumber(10), DynValue.NewNumber(5));

            await Assert.That(noArgs.Number).IsEqualTo(0d);
            await Assert.That(objectArgs.Number).IsEqualTo(5d);
            await Assert.That(dynValues.Number).IsEqualTo(15d);
        }

        [global::TUnit.Core.Test]
        public async Task UpValuesTypeIsNoneWhenNoUpValuesAreCaptured()
        {
            Script script = new();
            Closure closure = new(
                script,
                idx: 0,
                symbols: System.Array.Empty<SymbolRef>(),
                resolvedLocals: System.Array.Empty<DynValue>()
            );

            await Assert.That(closure.UpValuesCount).IsEqualTo(0);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(default(Closure.UpValuesType));
        }

        [global::TUnit.Core.Test]
        public async Task ContextPropertySurfacesCapturedUpValues()
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                local captured = 99
                return function()
                    return captured
                end
                "
            );

            Closure closure = function.Function;
            IReadOnlyList<DynValue> context = closure.Context;
            int capturedIndex = -1;

            for (int i = 0; i < closure.UpValuesCount; i++)
            {
                if (closure.GetUpValueName(i) == "captured")
                {
                    capturedIndex = i;
                    break;
                }
            }

            await Assert.That(context).IsNotNull();
            await Assert.That(context.Count).IsEqualTo(closure.UpValuesCount);
            await Assert.That(capturedIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(context[capturedIndex].Number).IsEqualTo(99d);
        }
    }
}
#pragma warning restore CA2007
