-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:170
-- @test: StringArithmeticCoercionTUnitTests.AllArithmeticOperationsWorkWithStrings
return '10' % 3
