-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:214
-- @test: MathNumericEdgeCasesTUnitTests.NegativeFloatDivisionByZeroReturnsNegativeInfinity
return -1.0 / 0
