-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:181
-- @test: TableModuleTUnitTests.SortNumbersIgnoresNumberMetatableForNumericPairs
local calls = 0
                debug.setmetatable(0, { __lt = function(a, b)
                    calls = calls + 1
                    return a > b
                end })
                local values = { 4.5, 1.25, 3 }
                table.sort(values)
                assert(values[1] == 1.25 and values[2] == 3 and values[3] == 4.5, 'numeric order')

                local nan = 0 / 0
                for _, pair in ipairs({{1, nan}, {nan, 1}, {1.5, nan}, {nan, 1.5}, {nan, nan}}) do
                    local firstIsNan = pair[1] ~= pair[1]
                    local secondIsNan = pair[2] ~= pair[2]
                    table.sort(pair)
                    assert((pair[1] ~= pair[1]) == firstIsNan, 'first NaN position')
                    assert((pair[2] ~= pair[2]) == secondIsNan, 'second NaN position')
                end

                assert(calls == 0, 'number metatable must not handle number pairs')
