-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:176
-- @test: MathNumericEdgeCasesTUnitTests.FloatDivisionByZeroReturnsPositiveInfinity
-- @compat-notes: Test targets Lua 5.3+
return 1.0 / 0
