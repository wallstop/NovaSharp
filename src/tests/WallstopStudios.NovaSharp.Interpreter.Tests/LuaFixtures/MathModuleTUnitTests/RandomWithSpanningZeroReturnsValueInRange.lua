-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1030
-- @test: MathModuleTUnitTests.RandomWithSpanningZeroReturnsValueInRange
return math.random(-5, 5)
