-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:617
-- @test: MathNumericEdgeCasesTUnitTests.TointegerOfMinintegerReturnsMininteger
-- Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+); Lua 5.3+: math.mininteger (5.3+)
return math.tointeger(math.mininteger)
