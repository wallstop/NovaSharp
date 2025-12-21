-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:138
-- @test: StringArithmeticCoercionTUnitTests.AllArithmeticOperationsWorkWithStrings
-- @compat-notes: Test targets Lua 5.1
return '10' + 1
