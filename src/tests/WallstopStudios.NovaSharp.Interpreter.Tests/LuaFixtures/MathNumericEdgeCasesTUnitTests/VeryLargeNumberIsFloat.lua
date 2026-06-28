-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:536
-- @test: MathNumericEdgeCasesTUnitTests.VeryLargeNumberIsFloat
-- Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
return math.type(1e100)
