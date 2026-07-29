-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:297
-- @test: StringArithmeticCoercionTUnitTests.NonNumericStringCausesArithmeticError
return 'hello' + 1
