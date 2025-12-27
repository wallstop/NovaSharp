-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:119
-- @test: TableModuleTUnitTests.SortTreatsComparatorFalseResultsAsEqual
local values = { 3, 1 }
                table.sort(values, function(a, b)
                    return false
                end)
                return values[1], values[2]
