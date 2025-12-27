-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:75
-- @test: TableModuleTUnitTests.SortThrowsWhenComparatorIsInvalidType
local values = { 1, 2 }
                    table.sort(values, {})
