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

        #region Data-Driven Edge Case Tests

        /// <summary>
        /// Data-driven tests for table.concat with various separators and indices.
        /// Tests boundary conditions and edge cases.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatBasicUsage(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {'a', 'b', 'c', 'd'}
                return table.concat(t)
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("abcd")
                .Because($"table.concat with no separator should join elements in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.concat with custom separator.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { ", ", "a, b, c, d", "comma separator" },
            new object[] { "-", "a-b-c-d", "dash separator" },
            new object[] { "", "abcd", "empty separator" },
            new object[] { "---", "a---b---c---d", "multi-char separator" }
        )]
        public async Task ConcatWithSeparator(
            LuaCompatibilityVersion version,
            string separator,
            string expected,
            string description
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                $@"
                local t = {{'a', 'b', 'c', 'd'}}
                return table.concat(t, '{separator}')
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo(expected)
                .Because($"table.concat: {description} in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.concat with explicit start and end indices.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { 2, 3, "b-c", "middle range" },
            new object[] { 1, 4, "a-b-c-d", "full range" },
            new object[] { 1, 1, "a", "single element" },
            new object[] { 3, 3, "c", "single middle element" },
            new object[] { 4, 4, "d", "last element only" }
        )]
        public async Task ConcatWithIndices(
            LuaCompatibilityVersion version,
            int startIndex,
            int endIndex,
            string expected,
            string description
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                $@"
                local t = {{'a', 'b', 'c', 'd'}}
                return table.concat(t, '-', {startIndex}, {endIndex})
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo(expected)
                .Because($"table.concat: {description} in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.concat returns empty string when end index is before start.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task ConcatEmptyRangeEndBeforeStart(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {'a', 'b', 'c', 'd'}
                return table.concat(t, '-', 3, 2)
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("")
                .Because(
                    $"table.concat with end before start should return empty string in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.insert at various positions.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task InsertAtEnd(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {1, 2, 3}
                table.insert(t, 4)
                return t[1], t[2], t[3], t[4], #t
                "
            );

            await Assert.That(result.Tuple[0].Number).IsEqualTo(1d).ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Number).IsEqualTo(2d).ConfigureAwait(false);
            await Assert.That(result.Tuple[2].Number).IsEqualTo(3d).ConfigureAwait(false);
            await Assert.That(result.Tuple[3].Number).IsEqualTo(4d).ConfigureAwait(false);
            await Assert
                .That(result.Tuple[4].Number)
                .IsEqualTo(4d)
                .Because($"table length should be 4 after insert in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.insert at specific positions shifts elements.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { 1, "x", "x-a-b-c", "insert at beginning" },
            new object[] { 2, "x", "a-x-b-c", "insert at second position" },
            new object[] { 3, "x", "a-b-x-c", "insert at third position" }
        )]
        public async Task InsertAtPosition(
            LuaCompatibilityVersion version,
            int position,
            string value,
            string expected,
            string description
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                $@"
                local t = {{'a', 'b', 'c'}}
                table.insert(t, {position}, '{value}')
                return table.concat(t, '-')
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo(expected)
                .Because($"table.insert: {description} in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.remove at various positions.
        /// </summary>
        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { 1, "a", "b-c-d", "remove first" },
            new object[] { 2, "b", "a-c-d", "remove second" },
            new object[] { 4, "d", "a-b-c", "remove last" }
        )]
        public async Task RemoveAtPosition(
            LuaCompatibilityVersion version,
            int position,
            string expectedRemoved,
            string expectedRemaining,
            string description
        )
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                $@"
                local t = {{'a', 'b', 'c', 'd'}}
                local removed = table.remove(t, {position})
                return removed, table.concat(t, '-')
                "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo(expectedRemoved)
                .Because(
                    $"table.remove: {description} should return '{expectedRemoved}' in {version}"
                )
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .IsEqualTo(expectedRemaining)
                .Because(
                    $"table.remove: remaining elements should be '{expectedRemaining}' in {version}"
                )
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.remove with no position removes last element.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RemoveDefaultPosition(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {'a', 'b', 'c'}
                local removed = table.remove(t)
                return removed, table.concat(t, '-'), #t
                "
            );

            await Assert
                .That(result.Tuple[0].String)
                .IsEqualTo("c")
                .Because($"table.remove with no position should return last element in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[1].String)
                .IsEqualTo("a-b")
                .Because($"remaining elements should be 'a-b' in {version}")
                .ConfigureAwait(false);
            await Assert
                .That(result.Tuple[2].Number)
                .IsEqualTo(2d)
                .Because($"table length should be 2 after remove in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.sort with custom comparator edge cases.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SortDescendingOrder(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {3, 1, 4, 1, 5, 9, 2, 6}
                table.sort(t, function(a, b) return a > b end)
                return table.concat(t, '-')
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("9-6-5-4-3-2-1-1")
                .Because($"table.sort with descending comparator should work in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.sort with string elements.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SortStringElements(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {'banana', 'apple', 'cherry', 'date'}
                table.sort(t)
                return table.concat(t, '-')
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("apple-banana-cherry-date")
                .Because($"table.sort should sort strings alphabetically in {version}")
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Tests table.sort with comparator that sorts by string length.
        /// </summary>
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task SortByStringLength(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            DynValue result = script.DoString(
                @"
                local t = {'a', 'bbb', 'cc', 'dddd'}
                table.sort(t, function(a, b) return #a < #b end)
                return table.concat(t, '-')
                "
            );

            await Assert
                .That(result.String)
                .IsEqualTo("a-cc-bbb-dddd")
                .Because($"table.sort by length should work in {version}")
                .ConfigureAwait(false);
        }

        #endregion
    }
}
