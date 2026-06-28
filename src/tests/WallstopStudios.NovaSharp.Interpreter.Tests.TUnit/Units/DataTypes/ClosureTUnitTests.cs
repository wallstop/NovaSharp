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
        public async Task ClosureWithNoGlobalsHasNoEnvUpvalue(LuaCompatibilityVersion version)
        {
            // For Lua 5.1: _ENV is always present for setfenv/getfenv compatibility, so upvalue count is 1.
            // For Lua 5.2+: _ENV is only included when globals are referenced, so upvalue count is 0.
            Script script = new(version);
            DynValue function = script.DoString("return function(a) return a end");

            Closure closure = function.Function;

            if (version == LuaCompatibilityVersion.Lua51)
            {
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
            else
            {
                await Assert.That(closure.UpValuesCount).IsEqualTo(0).ConfigureAwait(false);
                await Assert
                    .That(closure.CapturedUpValuesType)
                    .IsEqualTo(default(Closure.UpValuesType))
                    .ConfigureAwait(false);
            }
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
            // This closure captures x and y. For Lua 5.1, _ENV is also included.
            // For Lua 5.2+, _ENV is NOT included since no globals are referenced.
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

            if (version == LuaCompatibilityVersion.Lua51)
            {
                // Lua 5.1: _ENV + x + y = 3 upvalues
                await Assert.That(upvalueCount).IsEqualTo(3).ConfigureAwait(false);
                await Assert.That(envIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            }
            else
            {
                // Lua 5.2+: x + y = 2 upvalues (no _ENV)
                await Assert.That(upvalueCount).IsEqualTo(2).ConfigureAwait(false);
                await Assert.That(envIndex).IsEqualTo(-1).ConfigureAwait(false);
            }
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
                "return function(a, b, c, d) return (a or 0) + (b or 0) + (c or 0) + (d or 0) end"
            );
            Closure closure = function.Function;

            DynValue noArgs = closure.Call();
            DynValue objectArgs = closure.Call(2, 3);
            DynValue fourObjectArgs = closure.Call(1, 2, 3, 4);
            DynValue dynValues = closure.Call(DynValue.NewNumber(10), DynValue.NewNumber(5));

            await Assert.That(noArgs.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(objectArgs.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(fourObjectArgs.Number).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(dynValues.Number).IsEqualTo(15d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task NullCallBindsToExistingParamsArrayOverload(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue function = script.DoString("return function(...) return select('#', ...) end");
            Closure closure = function.Function;

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
                closure.Call(null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ObjectArrayCallPreservesSpreadAndSingleObjectForms(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue capture = script.DoString(
                "return function(...) return select('#', ...), type((...)), ... end"
            );
            Closure closure = capture.Function;
            object[] args = new object[] { 1, 2 };

            DynValue spread = closure.Call(args);
            await Assert.That(spread.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(spread.Tuple.Length).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(spread.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(spread.Tuple[1].String).IsEqualTo("number").ConfigureAwait(false);
            await Assert.That(spread.Tuple[2].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(spread.Tuple[3].Number).IsEqualTo(2d).ConfigureAwait(false);

            DynValue cast = closure.Call((object)args);
            await Assert.That(cast.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(cast.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert.That(cast.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(cast.Tuple[1].String).IsEqualTo("table").ConfigureAwait(false);
            await Assert.That(cast.Tuple[2].Type).IsEqualTo(DataType.Table).ConfigureAwait(false);
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
