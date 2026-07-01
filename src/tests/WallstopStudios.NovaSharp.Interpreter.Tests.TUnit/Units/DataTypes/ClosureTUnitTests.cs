namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
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
        public async Task ContextExposesCapturedUpValuesThroughReadOnlyList(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            DynValue function = script.DoString(
                @"
                local x = 7
                return function()
                    return x
                end
                "
            );

            Closure closure = function.Function;
            IReadOnlyList<DynValue> context = closure.Context;
            int xIndex = -1;

            for (int i = 0; i < closure.UpValuesCount; i++)
            {
                if (closure.GetUpValueName(i) == "x")
                {
                    xIndex = i;
                    break;
                }
            }

            await Assert.That(xIndex).IsGreaterThanOrEqualTo(0).ConfigureAwait(false);
            await Assert.That(context.Count).IsEqualTo(closure.UpValuesCount).ConfigureAwait(false);
            await Assert.That(context[xIndex].Number).IsEqualTo(7d).ConfigureAwait(false);

            closure.SetUpValue(xIndex, DynValue.NewNumber(11d));

            await Assert.That(context[xIndex].Number).IsEqualTo(11d).ConfigureAwait(false);

            IEnumerator<DynValue> enumerator = context.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);

            int enumeratedCount = 0;
            bool observedUpdatedSlot = false;
            while (enumerator.MoveNext())
            {
                if (enumeratedCount == xIndex)
                {
                    await Assert
                        .That(enumerator.Current.Number)
                        .IsEqualTo(11d)
                        .ConfigureAwait(false);
                    observedUpdatedSlot = true;
                }

                enumeratedCount++;
            }

            await Assert.That(enumeratedCount).IsEqualTo(context.Count).ConfigureAwait(false);
            await Assert.That(observedUpdatedSlot).IsTrue().ConfigureAwait(false);
            Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ClosureWithManyCapturedSymbolsPreservesUpValueMetadata(
            LuaCompatibilityVersion version
        )
        {
            const int capturedCount = 24;
            Script script = new(version);
            StringBuilder builder = new();
            int expectedSum = 0;

            for (int i = 1; i <= capturedCount; i++)
            {
                builder.Append("local captured_");
                builder.Append(i.ToString(CultureInfo.InvariantCulture));
                builder.Append(" = ");
                builder.AppendLine(i.ToString(CultureInfo.InvariantCulture));
                expectedSum += i;
            }

            builder.Append("return function() return ");
            for (int i = 1; i <= capturedCount; i++)
            {
                if (i > 1)
                {
                    builder.Append(" + ");
                }

                builder.Append("captured_");
                builder.Append(i.ToString(CultureInfo.InvariantCulture));
            }
            builder.AppendLine(" end");

            DynValue function = script.DoString(builder.ToString());
            Closure closure = function.Function;
            DynValue result = script.Call(function);

            int expectedUpValueCount =
                version == LuaCompatibilityVersion.Lua51 ? capturedCount + 1 : capturedCount;

            await Assert.That(result.Number).IsEqualTo(expectedSum).ConfigureAwait(false);
            await Assert
                .That(closure.UpValuesCount)
                .IsEqualTo(expectedUpValueCount)
                .ConfigureAwait(false);

            for (int i = 1; i <= capturedCount; i++)
            {
                int upValueIndex = version == LuaCompatibilityVersion.Lua51 ? i : i - 1;
                string expectedName = "captured_" + i.ToString(CultureInfo.InvariantCulture);

                await Assert
                    .That(closure.GetUpValueName(upValueIndex))
                    .IsEqualTo(expectedName)
                    .ConfigureAwait(false);
                await Assert
                    .That(closure.GetUpValue(upValueIndex).Number)
                    .IsEqualTo(i)
                    .ConfigureAwait(false);
            }
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
                "return function(a, b, c, d, e) return (a or 0) + (b or 0) + (c or 0) + (d or 0) + (e or 0) end"
            );
            Closure closure = function.Function;

            DynValue noArgs = closure.Call();
            DynValue oneDynValue = closure.Call(DynValue.NewNumber(12));
            DynValue objectArgs = closure.Call(2, 3);
            DynValue fourObjectArgs = closure.Call(1, 2, 3, 4);
            DynValue fiveObjectArgs = closure.Call(1, 2, 3, 4, 5);
            DynValue twoDynValues = closure.Call(DynValue.NewNumber(10), DynValue.NewNumber(5));
            DynValue threeDynValues = closure.Call(
                DynValue.NewNumber(10),
                DynValue.NewNumber(5),
                DynValue.NewNumber(2)
            );
            DynValue fourDynValues = closure.Call(
                DynValue.NewNumber(10),
                DynValue.NewNumber(5),
                DynValue.NewNumber(2),
                DynValue.NewNumber(1)
            );
            DynValue fiveDynValues = closure.Call(
                DynValue.NewNumber(10),
                DynValue.NewNumber(5),
                DynValue.NewNumber(2),
                DynValue.NewNumber(1),
                DynValue.NewNumber(4)
            );

            await Assert.That(noArgs.Number).IsEqualTo(0d).ConfigureAwait(false);
            await Assert.That(oneDynValue.Number).IsEqualTo(12d).ConfigureAwait(false);
            await Assert.That(objectArgs.Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(fourObjectArgs.Number).IsEqualTo(10d).ConfigureAwait(false);
            await Assert.That(fiveObjectArgs.Number).IsEqualTo(15d).ConfigureAwait(false);
            await Assert.That(twoDynValues.Number).IsEqualTo(15d).ConfigureAwait(false);
            await Assert.That(threeDynValues.Number).IsEqualTo(17d).ConfigureAwait(false);
            await Assert.That(fourDynValues.Number).IsEqualTo(18d).ConfigureAwait(false);
            await Assert.That(fiveDynValues.Number).IsEqualTo(22d).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(0)]
        [global::TUnit.Core.Arguments(1)]
        [global::TUnit.Core.Arguments(2)]
        [global::TUnit.Core.Arguments(3)]
        [global::TUnit.Core.Arguments(4)]
        [global::TUnit.Core.Arguments(5)]
        public async Task ReadOnlySpanCallInvokesUnderlyingFunction(int arity)
        {
            Script script = new();
            DynValue function = script.DoString(
                @"
                return function(...)
                    local sum = 0
                    for i = 1, select('#', ...) do
                        sum = sum + select(i, ...)
                    end
                    return sum
                end
                "
            );
            Closure closure = function.Function;
            DynValue[] args = new DynValue[arity];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = DynValue.NewNumber(i + 1d);
            }

            DynValue result = closure.Call(args.AsSpan());

            await Assert
                .That(result.Number)
                .IsEqualTo(arity * (arity + 1) / 2d)
                .ConfigureAwait(false);
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
                closure.Call((DynValue[])null)
            );

            await Assert.That(exception.ParamName).IsEqualTo("args").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task MultipleNullCallArgumentsRemainLuaNil(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue capture = script.DoString(
                @"
                return function(...)
                    local a, b, c, d, e = ...
                    return select('#', ...), a == nil, b == nil, c == nil, d == nil, e == nil
                end
                "
            );
            Closure closure = capture.Function;

            DynValue twoArgs = closure.Call(null, null);
            DynValue threeArgs = closure.Call(null, DynValue.NewNumber(3), null);
            DynValue fourArgs = closure.Call(null, null, null, null);
            DynValue fiveArgs = closure.Call((DynValue)null, null, null, null, null);
            DynValue paramsArrayArgs = closure.Call(
                new DynValue[] { null, DynValue.NewNumber(3), null }
            );

            await Assert.That(twoArgs.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(twoArgs.Tuple[0].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(twoArgs.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(twoArgs.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(twoArgs.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(twoArgs.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);

            await Assert.That(threeArgs.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(threeArgs.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(threeArgs.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(threeArgs.Tuple[2].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(threeArgs.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(threeArgs.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);

            await Assert.That(fourArgs.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[0].Number).IsEqualTo(4d).ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fourArgs.Tuple[5].Boolean).IsTrue().ConfigureAwait(false);

            await Assert.That(fiveArgs.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[0].Number).IsEqualTo(5d).ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[2].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(fiveArgs.Tuple[5].Boolean).IsTrue().ConfigureAwait(false);

            await Assert.That(paramsArrayArgs.Type).IsEqualTo(DataType.Tuple).ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[0].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[1].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[2].Boolean).IsFalse().ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[3].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[4].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(paramsArrayArgs.Tuple[5].Boolean).IsTrue().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task FixedDynValueCallOverloadsRejectForeignResources(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            Script foreignScript = new(version);
            DynValue function = script.DoString("return function(...) return ... end");
            Closure closure = function.Function;
            DynValue foreignTable = DynValue.NewTable(new Table(foreignScript));

            ScriptRuntimeException twoArgException = Assert.Throws<ScriptRuntimeException>(() =>
                closure.Call(DynValue.Nil, foreignTable)
            );
            ScriptRuntimeException threeArgException = Assert.Throws<ScriptRuntimeException>(() =>
                closure.Call(DynValue.Nil, DynValue.Nil, foreignTable)
            );
            ScriptRuntimeException fourArgException = Assert.Throws<ScriptRuntimeException>(() =>
                closure.Call(DynValue.Nil, DynValue.Nil, DynValue.Nil, foreignTable)
            );
            ScriptRuntimeException fiveArgException = Assert.Throws<ScriptRuntimeException>(() =>
                closure.Call(DynValue.Nil, DynValue.Nil, DynValue.Nil, DynValue.Nil, foreignTable)
            );

            await Assert.That(twoArgException).IsNotNull().ConfigureAwait(false);
            await Assert.That(threeArgException).IsNotNull().ConfigureAwait(false);
            await Assert.That(fourArgException).IsNotNull().ConfigureAwait(false);
            await Assert.That(fiveArgException).IsNotNull().ConfigureAwait(false);
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
