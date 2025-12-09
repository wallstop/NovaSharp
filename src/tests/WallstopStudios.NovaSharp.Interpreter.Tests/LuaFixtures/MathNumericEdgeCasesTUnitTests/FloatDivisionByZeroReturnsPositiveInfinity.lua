-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:153
-- @test: MathNumericEdgeCasesTUnitTests.FloatDivisionByZeroReturnsPositiveInfinity
-- @compat-notes: Test targets Lua 5.4+
return 1.0 / 0
