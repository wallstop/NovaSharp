-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:97
-- @test: MathNumericEdgeCasesTUnitTests.MinintegerNotAvailableInPreLua53
-- Test targets Lua 5.2+; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger
