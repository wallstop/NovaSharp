-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableSortTUnitTests.cs:86
-- @test: TableSortTUnitTests.SortChecksFullLengthBeforeDecidingWhetherToSort
local function check(length, diagnostic)
                    local values = setmetatable({}, { __len = function() return length end })
                    local ok, result = pcall(function()
                        return select('#', table.sort(values, false))
                    end)
                    if diagnostic then
                        assert(not ok and result:find(diagnostic, 1, true), tostring(length))
                    else
                        assert(ok and result == 0, 'nontrivial length check')
                    end
                end
                check(0.5, 'object length is not an integer')
                check(1 / 0, 'object length is not an integer')
                check(2147483647, 'array too big')
                check(2147483648, 'array too big')
                check(-2147483649)
