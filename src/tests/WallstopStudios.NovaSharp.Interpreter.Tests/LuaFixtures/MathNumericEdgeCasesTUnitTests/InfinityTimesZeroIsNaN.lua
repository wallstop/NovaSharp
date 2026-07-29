-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:367
-- @test: MathNumericEdgeCasesTUnitTests.InfinityTimesZeroIsNaN
return math.huge * 0
