namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableModuleTUnitTests
    {
        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task PackPreservesNilAndReportsCount(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task UnpackHonorsExplicitBounds(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [AllLuaVersions]
        public async Task SortNumbersUsesDefaultComparer(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [AllLuaVersions]
        public async Task SortThrowsWhenComparatorIsInvalidType(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [AllLuaVersions]
        public async Task SortUsesMetamethodWhenComparerMissing(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [AllLuaVersions]
        public async Task SortTreatsComparatorFalseResultsAsEqual(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
        [AllLuaVersions]
        public async Task SortThrowsWhenValuesHaveNoNaturalOrder(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [AllLuaVersions]
        public async Task SortPropagatesErrorsRaisedByComparator(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task InsertValidatesPositionType(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task RemoveIgnoresExtraArguments(LuaCompatibilityVersion version)
        {
            // Per Lua behavior across all versions, table.remove silently ignores extra arguments.
            // This is consistent with Lua 5.1, 5.2, 5.3, and 5.4.
            // Reference: Lua manual §6.6 (table.remove) - no argument count validation mentioned.
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task InsertUsesLenMetamethodWhenPresent(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MoveErrorsOnNonIntegerArgLua53Plus(LuaCompatibilityVersion version)
        {
            // table.move is Lua 5.3+ only, so always requires integer representation
            Script script = new Script(version, CoreModulePresets.Complete);

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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task InsertAcceptsIntegralFloatLua53Plus(LuaCompatibilityVersion version)
        {
            // Integral floats like 2.0 should be accepted
            Script script = new Script(version, CoreModulePresets.Complete);

            DynValue result = script.DoString(
                @"
                local t = {1, 2, 3}
                table.insert(t, 2.0, 'x')  -- 2.0 is integral
                return t[2]
            "
            );

            await Assert.That(result.String).IsEqualTo("x").ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.pack is available in Lua 5.2+.
        /// table.pack was added in Lua 5.2.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task PackAvailableInLua52Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.pack(1, 2, 3).n");

            await Assert
                .That(result.Number)
                .IsEqualTo(3)
                .Because($"table.pack should be available in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.pack is NOT available in Lua 5.1.
        /// table.pack was added in Lua 5.2.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task PackIsNilInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.pack");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"table.pack was added in Lua 5.2. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.unpack is available in Lua 5.2+.
        /// table.unpack was added in Lua 5.2 (moved from global unpack).
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task TableUnpackAvailableInLua52Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.unpack({10, 20, 30})");

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(10)
                .Because($"table.unpack should be available in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.unpack is NOT available in Lua 5.1.
        /// In Lua 5.1, use the global unpack function instead.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task TableUnpackIsNilInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.unpack");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"table.unpack was added in Lua 5.2. Use global unpack in 5.1. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that global unpack is available in Lua 5.1.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        public async Task GlobalUnpackAvailableInLua51(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return unpack({10, 20, 30})");

            await Assert.That(result.Tuple.Length).IsEqualTo(3).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[0].Number)
                .IsEqualTo(10)
                .Because("Global unpack should be available in Lua 5.1")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that global unpack is NOT available in Lua 5.2+.
        /// Global unpack was moved to table.unpack in Lua 5.2.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task GlobalUnpackIsNilInLua52Plus(
            Compatibility.LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return unpack");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"Global unpack was moved to table.unpack in Lua 5.2+. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.maxn is available in Lua 5.1 and 5.2.
        /// table.maxn was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua52)]
        public async Task MaxnAvailableInLua51And52(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.maxn({[5] = true, [3] = true})");

            await Assert
                .That(result.Number)
                .IsEqualTo(5)
                .Because($"table.maxn should return 5 in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests that table.maxn is NOT available in Lua 5.3+.
        /// table.maxn was deprecated in Lua 5.2 and removed in Lua 5.3.
        /// </summary>
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(Compatibility.LuaCompatibilityVersion.Lua55)]
        public async Task MaxnIsNilInLua53Plus(Compatibility.LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            DynValue result = script.DoString("return table.maxn");

            await Assert
                .That(result.IsNil())
                .IsTrue()
                .Because(
                    $"table.maxn was removed in Lua 5.3+. Actual type: {result.Type}, value: {result}"
                )
                .ConfigureAwait(false);
        }

        private static Script CreateScript()
        {
            return new Script(CoreModulePresets.Complete);
        }

        private static Script CreateScript(Compatibility.LuaCompatibilityVersion version)
        {
            return new Script(version, CoreModulePresets.Complete);
        }
    }
}
