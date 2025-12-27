-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1066
-- @test: MathModuleTUnitTests.RandomWithSingleValueRangeReturnsValue
return math.random(5, 5)
