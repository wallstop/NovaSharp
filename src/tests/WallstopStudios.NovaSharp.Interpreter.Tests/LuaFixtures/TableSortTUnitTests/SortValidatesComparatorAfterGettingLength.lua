-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableSortTUnitTests.cs:52
-- @test: TableSortTUnitTests.SortValidatesComparatorAfterGettingLength
local reads = 0
                local function check(length, shouldSucceed)
                    local values = setmetatable({2, 1}, { __len = function()
                        reads = reads + 1
                        return length
                    end })
                    local ok, result = pcall(function()
                        return select('#', table.sort(values, false))
                    end)
                    assert(ok == shouldSucceed, 'comparator validation')
                    if ok then
                        assert(result == 0, 'short sort return count')
                    else
                        assert(result:find('function expected', 1, true), 'invalid comparator diagnostic')
                    end
                    assert(values[1] == 2 and values[2] == 1, 'values before sorting')
                end

                check(1, _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2')
                check(2, false)
                assert(reads == (_VERSION == 'Lua 5.1' and 0 or 2), 'length reads')
