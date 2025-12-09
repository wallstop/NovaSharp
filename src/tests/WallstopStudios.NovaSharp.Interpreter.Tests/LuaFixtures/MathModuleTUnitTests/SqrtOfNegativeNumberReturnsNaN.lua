-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:95
-- @test: MathModuleTUnitTests.SqrtOfNegativeNumberReturnsNaN
return math.sqrt(-1)
