namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class ClosureTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetUpValuesTypeReturnsEnvironmentWhenOnlyEnvIsCaptured(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue function = script.DoString("return function(a) return a end");

            Closure closure = function.Function;

            await Assert.That(closure.UpValuesCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(closure.GetUpValueName(0))
                .IsEqualTo(WellKnownSymbols.ENV)
                .ConfigureAwait(false);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(Closure.UpValuesType.Environment)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MetadataPropertiesExposeScriptAndEntryPoint(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue function = script.DoString("return function() return 42 end");
            Closure closure = function.Function;

            await Assert.That(closure.OwnerScript).IsSameReferenceAs(script).ConfigureAwait(false);
            await Assert
                .That(closure.EntryPointByteCodeLocation)
                .IsGreaterThanOrEqualTo(0)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetUpValuesTypeDetectsEnvironmentUpValue(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue function = script.DoString("return function() return _ENV end");

            Closure closure = function.Function;

            await Assert.That(closure.UpValuesCount).IsEqualTo(1).ConfigureAwait(false);
            await Assert
                .That(closure.GetUpValueName(0))
                .IsEqualTo(WellKnownSymbols.ENV)
                .ConfigureAwait(false);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(Closure.UpValuesType.Environment)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task GetUpValuesExposesCapturedSymbols(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(upvalueCount).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(envIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(xIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(yIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(closure.GetUpValue(xIndex).Number)
                .IsEqualTo(3d)
                .ConfigureAwait(false);
            await Assert
                .That(closure.GetUpValue(yIndex).Number)
                .IsEqualTo(4d)
                .ConfigureAwait(false);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(Closure.UpValuesType.Closure)
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task DelegatesInvokeScriptFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue function = script.DoString("return function(a, b) return a + b end");
            Closure closure = function.Function;

            ScriptFunctionCallback generic = closure.GetDelegate();
            object genericResult = generic(1, 2);

            ScriptFunctionCallback<int> typed = closure.GetDelegate<int>();
            int typedResult = typed(5, 7);

            // genericResult may be long or double depending on internal Lua number representation
            await Assert
                .That(Convert.ToDouble(genericResult, CultureInfo.InvariantCulture))
                .IsEqualTo(3d)
                .ConfigureAwait(false);
            await Assert.That(typedResult).IsEqualTo(12).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task CallOverloadsInvokeUnderlyingFunction(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue function = script.DoString(
                "return function(a, b) return (a or 0) + (b or 0) end"
            );
            Closure closure = function.Function;

            DynValue noArgs = closure.Call();
            DynValue objectArgs = closure.Call(2, 3);
            DynValue dynValues = closure.Call(DynValue.NewNumber(10), DynValue.NewNumber(5));

            await Assert.That(noArgs.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(objectArgs.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(dynValues.Number).IsEqualTo(15d).ConfigureAwait(false);
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

            await Assert.That(closure.UpValuesCount).IsEqualTo(0).ConfigureAwait(false);
            await Assert
                .That(closure.CapturedUpValuesType)
                .IsEqualTo(default(Closure.UpValuesType))
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ContextPropertySurfacesCapturedUpValues(LuaCompatibilityVersion version)
        {
            Script script = new(version);
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

            await Assert.That(context).IsNotNull().ConfigureAwait(false);
            await Assert.That(context.Count).IsEqualTo(closure.UpValuesCount).ConfigureAwait(false);
            await Assert.That(capturedIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(context[capturedIndex].Number).IsEqualTo(99d).ConfigureAwait(false);
        }
    }
}
