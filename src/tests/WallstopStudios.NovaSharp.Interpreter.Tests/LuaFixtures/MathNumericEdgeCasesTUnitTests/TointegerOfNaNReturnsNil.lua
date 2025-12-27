-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:643
-- @test: MathNumericEdgeCasesTUnitTests.TointegerOfNaNReturnsNil
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(0/0)
