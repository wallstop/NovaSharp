-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:221
-- @test: MathNumericEdgeCasesTUnitTests.FloatDivisionByNegativeZeroReturnsNegativeInfinity
-- @compat-notes: Test targets Lua 5.1
return 1.0 / -0.0
