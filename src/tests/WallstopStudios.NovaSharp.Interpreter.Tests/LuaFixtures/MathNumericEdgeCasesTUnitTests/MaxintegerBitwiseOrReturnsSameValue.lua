-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:129
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerBitwiseOrReturnsSameValue
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise OR; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger | 0
