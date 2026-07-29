-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:129
-- @test: MathNumericEdgeCasesTUnitTests.MinintegerMinusOneWrapsToMaxinteger
-- Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger - 1
