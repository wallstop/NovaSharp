namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using System.Threading.Tasks;
    using global::NovaSharp;
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
        [AllLuaVersions]
        public async Task RegisteredTableCallbacksUseArgumentViews(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            Table library = script.Globals.Get("table").Table;
            string[] commonNames = { "concat", "insert", "remove", "sort" };

            foreach (string name in commonNames)
            {
                await Assert.That(library.Get(name).Type).IsEqualTo(DataType.ClrFunction);
            }

            bool hasTablePackAndUnpack = version >= LuaCompatibilityVersion.Lua52;
            bool hasTableMaxN = version <= LuaCompatibilityVersion.Lua52;
            bool hasTableMove = version >= LuaCompatibilityVersion.Lua53;
            await Assert.That(library.Get("pack").IsNil).IsEqualTo(!hasTablePackAndUnpack);
            await Assert.That(library.Get("unpack").IsNil).IsEqualTo(!hasTablePackAndUnpack);
            await Assert.That(library.Get("maxn").IsNil).IsEqualTo(!hasTableMaxN);
            await Assert.That(library.Get("move").IsNil).IsEqualTo(!hasTableMove);

            foreach (TablePair pair in library.GetNonNilPairsEnumerator())
            {
                await Assert.That(pair.Value.Type).IsEqualTo(DataType.ClrFunction);
                await Assert
                    .That(pair.Value.Callback.HasArgumentViewCallback)
                    .IsTrue()
                    .Because($"table.{pair.Key.String} should use stack-only arguments");
            }

            LuaValue globalUnpack = script.Globals.Get("unpack");
            if (version == LuaCompatibilityVersion.Lua51)
            {
                await Assert.That(globalUnpack.Type).IsEqualTo(DataType.ClrFunction);
                await Assert.That(globalUnpack.Callback.HasArgumentViewCallback).IsTrue();
            }
            else
            {
                await Assert.That(globalUnpack.IsNil).IsTrue();
            }

            script.DoString(
                @"
                local values = {3, 1}
                table.insert(values, 2, 2)
                table.sort(values)
                assert(table.remove(values, 2) == 2)
                assert(table.concat(values, '-') == '1-3')

                local unpackRange = table.unpack or unpack
                local first, last = unpackRange(values)
                assert(first == 1 and last == 3)
                assert(select('#', unpackRange(values, 2, 1)) == 0)
                assert(select('#', unpackRange(values, 2, 2)) == 1)

                if table.maxn then
                    assert(table.maxn(values) == 2, 'maxn ignores the removed array entry')
                    values[3.5] = true
                    assert(table.maxn(values) == 3.5, 'maxn includes fractional keys')
                    values[3.5] = nil
                    assert(table.maxn(values) == 2, 'maxn ignores the removed fractional key')
                end
                if _VERSION ~= 'Lua 5.1' then
                    local function items() return 1, nil, 3 end
                    local packed = table.pack(items())
                    assert(packed.n == 3 and packed[2] == nil and packed[3] == 3)
                end
                if _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2' then
                    assert(table.move(values, 1, 2, 2) == values)
                    assert(values[2] == 1 and values[3] == 3)
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task PackPreservesNilAndReportsCount(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local t = table.pack('a', nil, 42)
                return t.n, t[1], t[2], t[3]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(4);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(3);
            await Assert.That(result.Tuple[1].String).IsEqualTo("a");
            await Assert.That(result.Tuple[2].IsNil).IsTrue();
            await Assert.That(result.Tuple[3].Number).IsEqualTo(42);
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task PackPreservesExpandedNilAndReportsCount(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
                @"
                local function values()
                    return 'a', nil, 'c'
                end

                local packed = table.pack('head', values())
                assert(packed.n == 4, 'packed count')
                assert(packed[1] == 'head', 'packed head')
                assert(packed[2] == 'a', 'packed expanded first')
                assert(packed[3] == nil, 'packed expanded nil')
                assert(packed[4] == 'c', 'packed expanded third')

                return packed.n, packed[1], packed[2], packed[3], packed[4]
                "
            );

            await Assert.That(result.Tuple.Length).IsEqualTo(5);
            await Assert.That(result.Tuple[0].Number).IsEqualTo(4);
            await Assert.That(result.Tuple[1].String).IsEqualTo("head");
            await Assert.That(result.Tuple[2].String).IsEqualTo("a");
            await Assert.That(result.Tuple[3].IsNil).IsTrue();
            await Assert.That(result.Tuple[4].String).IsEqualTo("c");
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua52)]
        public async Task UnpackHonorsExplicitBounds(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
        public void SortNumbersIgnoresNumberMetatableForNumericPairs(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local calls = 0
                debug.setmetatable(0, { __lt = function(a, b)
                    calls = calls + 1
                    return a > b
                end })
                local values = { 4.5, 1.25, 3 }
                table.sort(values)
                assert(values[1] == 1.25 and values[2] == 3 and values[3] == 4.5, 'numeric order')
                assert(calls == 0, 'number metatable must not handle number pairs')
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public void SortNumbersPreservesExactIntegerAndMixedOrdering(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local function check(name, low, high)
                    local values = {high, low}
                    table.sort(values)
                    assert(math.type(values[1]) == math.type(low) and values[1] == low, name .. ': low')
                    assert(math.type(values[2]) == math.type(high) and values[2] == high, name .. ': high')

                    table.sort(values)
                    assert(math.type(values[1]) == math.type(low) and values[1] == low, name .. ': repeated low')
                    assert(math.type(values[2]) == math.type(high) and values[2] == high, name .. ': repeated high')
                end

                check('adjacent integers above 2^53', 9007199254740992, 9007199254740993)
                check('integer above rounded float', 9007199254740992.0, 9007199254740993)
                check('integer below rounded negative float', -9007199254740993, -9007199254740992.0)
                check('integer below rounded-up float', 9007199254740995, 9007199254740996.0)
                check('adjacent minimum integers', math.mininteger, math.mininteger + 1)
                check('adjacent maximum integers', math.maxinteger - 1, math.maxinteger)
                check('float equal to minimum integer below next integer', -9223372036854775808.0, math.mininteger + 1)
                check('maximum integer below first out-of-range float', math.maxinteger, 9223372036854775808.0)
                check('float below minimum integer', -9223372036854777856.0, math.mininteger)
                check('maximum integer above nearest in-range float', 9223372036854774784.0, math.maxinteger)
                check('negative fractional float', -1.5, -1)
                check('positive fractional float', 1, 1.5)
                check('negative infinity', -math.huge, math.mininteger)
                check('positive infinity', math.maxinteger, math.huge)

                local values = {9007199254740992, 9007199254740993}
                table.sort(values, function(a, b) return a > b end)
                assert(values[1] == 9007199254740993 and values[2] == 9007199254740992, 'custom comparator')
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsUntil(LuaCompatibilityVersion.Lua52)]
        public async Task SortNumbersUsesFloatingPointOrderForHostIntegers(
            LuaCompatibilityVersion version
        )
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            LuaValue high = LuaValue.FromInteger(9007199254740993);
            LuaValue low = LuaValue.FromInteger(9007199254740992);
            Table values = new Table(script);
            values.Set(1, high);
            values.Set(2, low);

            LuaValue sort = script.Globals.Get("table").Table.Get("sort");
            script.CallValues(sort, LuaValue.NewTable(values));

            // Both operands compare as the same double in these profiles.
            await Assert
                .That(values.Get(1).LuaNumber.AsInteger)
                .IsEqualTo(high.LuaNumber.AsInteger);
            await Assert.That(values.Get(2).LuaNumber.AsInteger).IsEqualTo(low.LuaNumber.AsInteger);
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
                @"
                local values = setmetatable({ [1] = 'seed' }, {
                    __len = function()
                        return 4
                    end
                })

                table.insert(values, 'sentinel')
                return values[_VERSION == 'Lua 5.1' and 2 or 5]
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
            LuaValue result = script.DoString(
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

            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString("return table.pack(1, 2, 3).n");

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
            LuaValue result = script.DoString("return table.pack");

            await Assert
                .That(result.IsNil)
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
            LuaValue result = script.DoString("return table.unpack({10, 20, 30})");

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
            LuaValue result = script.DoString("return table.unpack");

            await Assert
                .That(result.IsNil)
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
            LuaValue result = script.DoString("return unpack({10, 20, 30})");

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
            LuaValue result = script.DoString("return unpack");

            await Assert
                .That(result.IsNil)
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
            LuaValue result = script.DoString("return table.maxn({[5] = true, [3] = true})");

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
            LuaValue result = script.DoString("return table.maxn");

            await Assert
                .That(result.IsNil)
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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
            LuaValue result = script.DoString(
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

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void InsertUsesArgumentCountAndReturnsNoValues(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.DoString(
                @"
                local function none() end
                local function pair() return 2, nil end
                local function extra() return 2, 4, 6 end

                local appended = {3, 5}
                assert(select('#', table.insert(appended, 7, none())) == 0, 'append result count')
                assert(appended[1] == 3 and appended[2] == 5 and appended[3] == 7)
                assert(select('#', table.insert(appended, nil)) == 0, 'append nil result count')
                assert(appended[3] == 7 and appended[4] == nil)

                local inserted = {3, 5}
                assert(select('#', table.insert(inserted, 2, 4)) == 0, 'insert result count')
                assert(inserted[1] == 3 and inserted[2] == 4 and inserted[3] == 5)
                assert(select('#', table.insert(inserted, 2, nil)) == 0, 'explicit nil result count')
                assert(inserted[1] == 3 and inserted[2] == nil and inserted[3] == 4 and inserted[4] == 5)

                local expanded = {3, 5}
                assert(select('#', table.insert(expanded, pair())) == 0, 'expanded result count')
                assert(expanded[1] == 3 and expanded[2] == nil and expanded[3] == 5 and expanded[4] == nil)

                local rejected = {3, 5}
                local ok, message = pcall(function() table.insert(rejected, none()) end)
                assert(not ok and string.find(message, ""wrong number of arguments"", 1, true), 'too few arguments')
                ok, message = pcall(function() table.insert(rejected, extra()) end)
                assert(not ok and string.find(message, ""wrong number of arguments"", 1, true), 'too many arguments')
                assert(rejected[1] == 3 and rejected[2] == 5 and rejected[3] == nil)

                local lengthCalls = 0
                local measured = setmetatable({}, {__len = function()
                    lengthCalls = lengthCalls + 1
                    return 0
                end})
                assert(not pcall(table.insert, measured))
                assert(lengthCalls == (_VERSION == 'Lua 5.1' and 0 or 1), 'length before arity')

                local binaryPosition = {3, 5}
                ok = pcall(table.insert, binaryPosition, '2' .. string.char(0) .. 'tail', 4)
                if _VERSION == 'Lua 5.1' then
                    assert(ok and binaryPosition[2] == 4 and binaryPosition[3] == 5)
                else
                    assert(not ok and binaryPosition[2] == 5 and binaryPosition[3] == nil)
                end

                local unicodePosition = {3, 5}
                ok = pcall(table.insert, unicodePosition, ' 2 ', 4)
                assert(not ok and unicodePosition[2] == 5 and unicodePosition[3] == nil)

                local positioned = {3, 5}
                table.insert(positioned, '2', 4)
                assert(positioned[1] == 3 and positioned[2] == 4 and positioned[3] == 5)
                ok, message = pcall(function() table.insert(positioned, 0, 2) end)
                if _VERSION == 'Lua 5.1' then
                    assert(ok and positioned[0] == 2 and positioned[1] == nil and positioned[2] == 3 and positioned[3] == 4 and positioned[4] == 5)
                    table.insert(positioned, 6, 6)
                    assert(positioned[6] == 6)
                    local fractional = {3, 5}
                    table.insert(fractional, -0.5, 2)
                    assert(fractional[-1] == nil and fractional[0] == 2 and fractional[2] == 3)

                    local sparse = {3, 5, [-101] = 'unchanged', [-99] = 'a', [-98] = 'b',
                        [-2] = 'c', [0] = 'd', [-50.5] = 'fractional'}
                    table.insert(sparse, -100, 'new')
                    assert(sparse[-101] == 'unchanged' and sparse[-100] == 'new')
                    assert(sparse[-99] == nil and sparse[-98] == 'a' and sparse[-97] == 'b')
                    assert(sparse[-2] == nil and sparse[-1] == 'c' and sparse[0] == nil)
                    assert(sparse[1] == 'd' and sparse[2] == 3 and sparse[3] == 5)
                    assert(sparse[-50.5] == 'fractional')
                else
                    assert(not ok and string.find(message, ""position out of bounds"", 1, true))
                    ok, message = pcall(function() table.insert(positioned, 6, 6) end)
                    assert(not ok and string.find(message, ""position out of bounds"", 1, true))
                    assert(positioned[1] == 3 and positioned[2] == 4 and positioned[3] == 5 and positioned[6] == nil)
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void InsertAppendUsesProfileLengthSemantics(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.DoString(
                @"
                local lua51 = _VERSION == 'Lua 5.1'
                local lua52 = _VERSION == 'Lua 5.2'
                local invalidLength = lua52 and 'object length is not a number'
                    or 'object length is not an integer'

                local function check(length, index, expectedError)
                    local calls = 0
                    local values = {}
                    setmetatable(values, {__len = function(first, second)
                        assert(first == values and second == values, 'length operands')
                        calls = calls + 1
                        return length
                    end})

                    local ok, result = pcall(function()
                        return select('#', table.insert(values, 'new'))
                    end)
                    if lua51 then
                        assert(ok and result == 0 and calls == 0 and rawget(values, 1) == 'new')
                    elseif expectedError then
                        assert(not ok and calls == 1 and next(values) == nil, 'invalid length')
                        assert(string.find(result, expectedError, 1, true), result)
                    else
                        assert(ok and result == 0 and calls == 1, 'append length')
                        assert(rawget(values, index) == 'new', 'append index')
                    end
                end

                check(-2, -1)
                check(-1, 0)
                check('2', 3)
                check(false, nil, invalidLength)
                check('2' .. string.char(0) .. 'tail', nil, invalidLength)
                check(2.75, 3, not lua52 and invalidLength)
                check(2147483647, lua52 and -2147483648 or 2147483648)
                check(4294967296, lua52 and 1 or 4294967297)
                if math.maxinteger then
                    check(math.maxinteger, math.mininteger)
                end
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void InsertPositionalUsesProfileLengthSemantics(LuaCompatibilityVersion version)
        {
            Script script = CreateScript(version);
            script.DoString(
                @"
                local lua51 = _VERSION == 'Lua 5.1'
                local lua52 = _VERSION == 'Lua 5.2'

                if not lua51 then
                    local wide = setmetatable({}, {__len = function() return 4294967298 end})
                    local endIndex = lua52 and 3 or 4294967299
                    assert(select('#', table.insert(wide, endIndex, 'new')) == 0, 'wide position')
                    assert(rawget(wide, endIndex) == 'new')

                    local shorter = setmetatable({3, 5, 7}, {__len = function() return 2.75 end})
                    local ok, errorText = pcall(table.insert, shorter, 2, 'new')
                    if lua52 then
                        assert(ok and shorter[2] == 'new' and shorter[3] == 5)
                    else
                        assert(not ok and shorter[2] == 5 and shorter[3] == 7)
                        assert(string.find(errorText, 'object length is not an integer', 1, true))
                    end

                    local calls = 0
                    local sparse = {[1] = 'head', [50] = 'middle', [101] = 'removed', [102] = 'kept'}
                    setmetatable(sparse, {__len = function(first, second)
                        assert(first == sparse and second == sparse, 'length operands')
                        calls = calls + 1
                        return 100
                    end})
                    table.insert(sparse, 2, 'new')
                    assert(calls == 1 and sparse[1] == 'head' and sparse[2] == 'new')
                    assert(sparse[50] == nil and sparse[51] == 'middle')
                    assert(sparse[101] == nil and sparse[102] == 'kept')
                end

                if not lua51 and not lua52 then
                    local high = 4294967298
                    local t = setmetatable({[high] = 'high', [high - 1] = 'previous'}, {
                        __len = function() return high end
                    })
                    table.insert(t, high - 1, 'new')
                    assert(t[high - 1] == 'new' and t[high] == 'previous' and t[high + 1] == 'high')

                    local unsignedBounds = _VERSION ~= 'Lua 5.3'
                    local wrapped = setmetatable({}, {__len = function() return math.maxinteger end})
                    local ok = pcall(table.insert, wrapped, 1, 'new')
                    assert(ok == unsignedBounds)
                    if unsignedBounds then
                        assert(wrapped[1] == 'new')
                    else
                        assert(next(wrapped) == nil)
                    end

                    local negative = setmetatable({[-2] = 'prior'}, {__len = function() return -2 end})
                    ok = pcall(table.insert, negative, -2, 'new')
                    assert(ok == unsignedBounds)
                    if unsignedBounds then
                        assert(negative[-2] == 'new' and negative[-1] == 'prior')
                    else
                        assert(negative[-2] == 'prior' and negative[-1] == nil)
                    end
                    assert(not pcall(table.insert, negative, 0, 'invalid'))
                end
                "
            );
        }

        #endregion
    }
}
