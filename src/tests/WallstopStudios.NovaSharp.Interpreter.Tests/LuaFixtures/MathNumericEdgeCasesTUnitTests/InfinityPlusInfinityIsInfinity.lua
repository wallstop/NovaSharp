-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:336
-- @test: MathNumericEdgeCasesTUnitTests.InfinityPlusInfinityIsInfinity
return math.huge + math.huge
