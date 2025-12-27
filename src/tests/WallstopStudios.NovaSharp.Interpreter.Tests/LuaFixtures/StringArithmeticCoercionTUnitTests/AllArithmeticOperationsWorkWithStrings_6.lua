-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:175
-- @test: StringArithmeticCoercionTUnitTests.AllArithmeticOperationsWorkWithStrings
-- @compat-notes: Test targets Lua 5.3+
return -'10'
