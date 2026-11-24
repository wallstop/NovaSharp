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
                assert(calls == 0, 'number metatable must not handle number pairs')
