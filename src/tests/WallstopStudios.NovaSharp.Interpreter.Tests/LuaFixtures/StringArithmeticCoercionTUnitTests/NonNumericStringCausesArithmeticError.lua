-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:317
-- @test: StringArithmeticCoercionTUnitTests.NonNumericStringCausesArithmeticError
-- @compat-notes: Test targets Lua 5.1
return 'hello' + 1
