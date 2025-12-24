-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:140
-- @test: TableModuleTUnitTests.SortTreatsComparatorFalseResultsAsEqual
-- @compat-notes: Test targets Lua 5.1
local values = { 3, 1 }
                table.sort(values, function(a, b)
                    return false
                end)
                return values[1], values[2]
