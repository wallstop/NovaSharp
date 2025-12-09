-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:104
-- @test: MathModuleTUnitTests.PowWithLargeExponentReturnsInfinity
return math.pow(10, 309)
