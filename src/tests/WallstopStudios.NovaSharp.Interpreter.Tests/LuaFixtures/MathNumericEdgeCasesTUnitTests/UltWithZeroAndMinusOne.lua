-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:801
-- @test: MathNumericEdgeCasesTUnitTests.UltWithZeroAndMinusOne
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.ult (5.3+)
return math.ult(0, -1)
