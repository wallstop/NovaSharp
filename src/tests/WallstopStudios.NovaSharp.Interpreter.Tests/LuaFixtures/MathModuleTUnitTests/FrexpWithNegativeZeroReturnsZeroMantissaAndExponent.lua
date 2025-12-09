-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:247
-- @test: MathModuleTUnitTests.FrexpWithNegativeZeroReturnsZeroMantissaAndExponent
return math.frexp(-0.0)
