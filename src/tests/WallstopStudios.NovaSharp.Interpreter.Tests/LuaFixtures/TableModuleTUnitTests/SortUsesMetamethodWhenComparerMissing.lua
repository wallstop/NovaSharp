-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:108
-- @test: TableModuleTUnitTests.SortUsesMetamethodWhenComparerMissing
-- @compat-notes: Test targets Lua 5.1
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
