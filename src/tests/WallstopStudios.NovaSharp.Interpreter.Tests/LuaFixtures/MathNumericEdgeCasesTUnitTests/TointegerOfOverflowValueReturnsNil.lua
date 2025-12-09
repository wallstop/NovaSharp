-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:538
-- @test: MathNumericEdgeCasesTUnitTests.TointegerOfOverflowValueReturnsNil
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(2^63)
