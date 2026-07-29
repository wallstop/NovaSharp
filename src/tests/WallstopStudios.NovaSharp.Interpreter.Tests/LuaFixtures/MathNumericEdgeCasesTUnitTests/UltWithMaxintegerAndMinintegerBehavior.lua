-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:672
-- @test: MathNumericEdgeCasesTUnitTests.UltWithMaxintegerAndMinintegerBehavior
-- Test targets Lua 5.3+; Lua 5.3+: math.ult (5.3+); Lua 5.3+: math.maxinteger (5.3+); Lua 5.3+: math.mininteger (5.3+)
return math.ult(math.maxinteger, math.mininteger)
