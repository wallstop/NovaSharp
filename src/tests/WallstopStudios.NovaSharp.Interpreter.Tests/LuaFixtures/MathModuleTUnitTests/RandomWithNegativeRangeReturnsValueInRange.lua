-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1090
-- @test: MathModuleTUnitTests.RandomWithNegativeRangeReturnsValueInRange
return math.random(-10, -5)
