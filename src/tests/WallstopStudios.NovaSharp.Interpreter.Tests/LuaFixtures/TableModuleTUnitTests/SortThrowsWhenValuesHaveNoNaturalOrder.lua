-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:143
-- @test: TableModuleTUnitTests.SortThrowsWhenValuesHaveNoNaturalOrder
table.sort({ true, false })
