-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:134
-- @test: TableModuleTUnitTests.SortThrowsWhenValuesHaveNoNaturalOrder
table.sort({ true, false })
