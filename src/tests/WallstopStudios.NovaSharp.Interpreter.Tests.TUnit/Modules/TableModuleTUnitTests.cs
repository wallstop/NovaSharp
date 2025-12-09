namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    public sealed class TableModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task PackPreservesNilAndReportsCount()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[1].String).IsEqualTo("a");
            await Assert.That(result.Tuple[2].IsNil()).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        public async Task UnpackHonorsExplicitBounds()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 10, 20, 30, 40 }
                return table.unpack(values, 2, 3)
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(2);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(20);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(30);
        }

        [global::TUnit.Core.Test]
        public async Task SortNumbersUsesDefaultComparer()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 4, 1, 3 }
                table.sort(values)
                return values[1], values[2], values[3]
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(4);
        }

        [global::TUnit.Core.Test]
        public async Task SortThrowsWhenComparatorIsInvalidType()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = { 1, 2 }
                    table.sort(values, {})
                    "
                )
            );

            await Assert.That(exception.Message).Contains("bad argument #2 to 'sort'");
        }

        [global::TUnit.Core.Test]
        public async Task SortUsesMetamethodWhenComparerMissing()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local mt = {}
                function mt.__lt(left, right)
                    return left.value < right.value
                end

                local values = {
                    setmetatable({ value = 3 }, mt),
                    setmetatable({ value = 1 }, mt),
                    setmetatable({ value = 2 }, mt)
                }

                table.sort(values)
                return values[1].value, values[2].value, values[3].value
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3);
        }

        [global::TUnit.Core.Test]
        public async Task SortTreatsComparatorFalseResultsAsEqual()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = { 3, 1 }
                table.sort(values, function(a, b)
                    return false
                end)
                return values[1], values[2]
                "
            );

            double first = result.Tuple[0].Number;
            double second = result.Tuple[1].Number;

            await Assert.That(first + second).IsEqualTo(4d);
            await Assert.That(first * second).IsEqualTo(3d);
        }

        [global::TUnit.Core.Test]
        public async Task SortThrowsWhenValuesHaveNoNaturalOrder()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    table.sort({ true, false })
                    "
                )
            );

            await Assert.That(exception.Message).Contains("attempt to compare");
        }

        [global::TUnit.Core.Test]
        public async Task SortPropagatesErrorsRaisedByComparator()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = { 1, 2 }
                    table.sort(values, function()
                        error('sort failed')
                    end)
                    "
                )
            );

            await Assert.That(exception.Message).Contains("sort failed");
        }

        [global::TUnit.Core.Test]
        public async Task InsertValidatesPositionType()
        {
            Script script = CreateScript();

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString(
                    @"
                    local values = {}
                    table.insert(values, 'two', 99)
                    "
                )
            );

            await Assert.That(exception.Message).Contains("table.insert");
        }

        [global::TUnit.Core.Test]
        public async Task RemoveIgnoresExtraArguments()
        {
            // Per Lua behavior across all versions, table.remove silently ignores extra arguments.
            // This is consistent with Lua 5.1, 5.2, 5.3, and 5.4.
            // Reference: Lua manual §6.6 (table.remove) - no argument count validation mentioned.
            Script script = CreateScript();

            DynValue result = script.DoString(
                @"
                local values = { 1, 2, 3, 4, 5 }
                local removed = table.remove(values, 1, 'extra', 'args', 999)
                return removed, #values, values[1]
                "
            );

            // Verify table.remove worked normally, ignoring extra arguments
            await Assert.That(result.Tuple[0].Number).IsEqualTo(1).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(4).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(2).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InsertUsesLenMetamethodWhenPresent()
        {
            Script script = CreateScript();
            DynValue result = script.DoString(
                @"
                local values = setmetatable({ [1] = 'seed' }, {
                    __len = function()
                        return 4
                    end
                })

                table.insert(values, 'sentinel')
                return values[5]
                "
            );

            await Assert.That(result.String).IsEqualTo("sentinel");
        }

        // ==========================================================================
        // Integer representation tests (Lua 5.3+ semantics)
        // ==========================================================================

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
        public async Task InsertErrorsOnNonIntegerPositionLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("table.insert({1,2,3}, 1.5, 'x')"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        public async Task InsertTruncatesNonIntegerPositionLua51And52(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            // table.insert({1,2,3}, 1.9, 'x') should truncate 1.9 to 1, inserting at position 1
            DynValue result = script.DoString(
                @"
                local t = {1, 2, 3}
                table.insert(t, 1.9, 'x')
                return t[1]
            "
            );

            await Assert.That(result.String).IsEqualTo("x").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
        public async Task RemoveErrorsOnNonIntegerPositionLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("table.remove({1,2,3}, 1.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
        public async Task ConcatErrorsOnNonIntegerIndexLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("table.concat({'a','b','c'}, '', 1.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Latest)]
        public async Task UnpackErrorsOnNonIntegerIndexLua53Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = CreateScript();
            script.Options.CompatibilityVersion = version;

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("table.unpack({1,2,3}, 1.5)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MoveErrorsOnNonIntegerArgLua53Plus()
        {
            // table.move is Lua 5.3+ only, so always requires integer representation
            Script script = CreateScript();
            script.Options.CompatibilityVersion = Compatibility.LuaCompatibilityVersion.Lua54;

            ScriptRuntimeException ex = await Assert
                .ThrowsAsync<ScriptRuntimeException>(async () =>
                    await Task.FromResult(script.DoString("table.move({1,2,3}, 1.5, 2, 1)"))
                        .ConfigureAwait(false)
                )
                .ConfigureAwait(false);
            await Assert
                .That(ex.Message)
                .Contains("number has no integer representation")
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InsertAcceptsIntegralFloatLua53Plus()
        {
            // Integral floats like 2.0 should be accepted
            Script script = CreateScript();
            script.Options.CompatibilityVersion = Compatibility.LuaCompatibilityVersion.Lua54;

            DynValue result = script.DoString(
                @"
                local t = {1, 2, 3}
                table.insert(t, 2.0, 'x')  -- 2.0 is integral
                return t[2]
            "
            );

            await Assert.That(result.String).IsEqualTo("x").ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModules.PresetComplete);
        }
    }
}
