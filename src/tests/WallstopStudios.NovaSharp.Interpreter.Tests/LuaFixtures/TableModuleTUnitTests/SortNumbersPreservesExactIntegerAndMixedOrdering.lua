-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:213
-- @test: TableModuleTUnitTests.SortNumbersPreservesExactIntegerAndMixedOrdering
-- Compatibility notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+); Lua 5.3+: math.maxinteger (5.3+); Lua 5.3+: math.mininteger (5.3+)
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
