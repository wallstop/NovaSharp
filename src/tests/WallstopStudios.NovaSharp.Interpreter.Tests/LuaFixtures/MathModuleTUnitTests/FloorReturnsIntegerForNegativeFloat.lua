-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:324
-- @test: MathModuleTUnitTests.FloorReturnsIntegerForNegativeFloat
return math.floor(-3.7)
