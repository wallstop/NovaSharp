-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:232
-- @test: MathNumericEdgeCasesTUnitTests.ZeroDividedByZeroReturnsNaN
-- Test targets Lua 5.3+
return 0 / 0
