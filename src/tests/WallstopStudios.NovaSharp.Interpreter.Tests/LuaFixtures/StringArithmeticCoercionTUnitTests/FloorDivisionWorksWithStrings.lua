-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs:205
-- @test: StringArithmeticCoercionTUnitTests.FloorDivisionWorksWithStrings
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: floor division
return '10' // 3
