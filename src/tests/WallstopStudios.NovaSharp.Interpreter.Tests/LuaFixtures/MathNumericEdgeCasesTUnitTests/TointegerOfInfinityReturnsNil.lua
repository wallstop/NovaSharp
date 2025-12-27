-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:630
-- @test: MathNumericEdgeCasesTUnitTests.TointegerOfInfinityReturnsNil
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(math.huge)
