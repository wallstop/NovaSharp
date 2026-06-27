-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:433
-- @test: MathNumericEdgeCasesTUnitTests.MathTypeReturnsFloatForInfinity
-- Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
return math.type(math.huge)
