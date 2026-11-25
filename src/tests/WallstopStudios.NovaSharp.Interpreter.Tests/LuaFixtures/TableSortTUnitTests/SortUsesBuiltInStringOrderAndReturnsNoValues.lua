-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableSortTUnitTests.cs:14
-- @test: TableSortTUnitTests.SortUsesBuiltInStringOrderAndReturnsNoValues
local calls = 0
                debug.setmetatable('', { __lt = function(a, b)
                    calls = calls + 1
                    return a > b
                end })

                local values = {'c', 'a', 'b'}
                local count = select('#', table.sort(values))
                assert(table.concat(values) == 'abc', 'default string order')
                assert(calls == 0, 'string pairs bypass __lt')
                assert(count == 0, 'default sort return count')

                local function descending(a, b) return a > b end
                assert(select('#', table.sort(values, descending)) == 0, 'explicit sort return count')
                assert(table.concat(values) == 'cba', 'explicit comparator')

                local mt = { __lt = function(a, b) return a.rank < b.rank end }
                local first = setmetatable({rank = 1}, mt)
                local second = setmetatable({rank = 2}, mt)
                local objects = {second, first}
                assert(select('#', table.sort(objects)) == 0, 'metamethod sort return count')
                assert(objects[1] == first and objects[2] == second, 'metamethod order')

                assert(select('#', table.sort({})) == 0, 'empty sort return count')
                local singleton = {'only'}
                assert(select('#', table.sort(singleton)) == 0, 'singleton sort return count')
                assert(singleton[1] == 'only', 'singleton value')

                local function fails() error('protected comparison') end
                local function succeeds() end
                local callbacks = {fails, succeeds}
                table.sort(callbacks, pcall)
                assert(callbacks[1] == succeeds, 'native comparator first result')

                local function compare(a, b) return a > b, true end
                local numbers = {1, 2}
                table.sort(numbers, compare)
                assert(numbers[1] == 2, 'Lua comparator first result')
