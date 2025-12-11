-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:111
-- @test: TableModuleTUnitTests.SortTreatsComparatorFalseResultsAsEqual
-- @compat-notes: Lua 5.3+: bitwise operators
local values = { 3, 1 }
                table.sort(values, function(a, b)
                    return false
                end)
                return values[1], values[2]
