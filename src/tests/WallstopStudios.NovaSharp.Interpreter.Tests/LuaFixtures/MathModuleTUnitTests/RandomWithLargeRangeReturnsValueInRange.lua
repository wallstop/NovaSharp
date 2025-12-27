-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1132
-- @test: MathModuleTUnitTests.RandomWithLargeRangeReturnsValueInRange
return math.random(1, 1000000)
