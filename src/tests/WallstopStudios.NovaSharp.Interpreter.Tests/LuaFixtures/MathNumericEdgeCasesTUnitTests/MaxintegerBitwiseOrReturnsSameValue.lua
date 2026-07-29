-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:145
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerBitwiseOrReturnsSameValue
-- Test targets Lua 5.3+; Lua 5.3+: bitwise OR; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger | 0
