-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:195
-- @test: MathNumericEdgeCasesTUnitTests.FloatDivisionByNegativeZeroReturnsNegativeInfinity
return 1.0 / -0.0
