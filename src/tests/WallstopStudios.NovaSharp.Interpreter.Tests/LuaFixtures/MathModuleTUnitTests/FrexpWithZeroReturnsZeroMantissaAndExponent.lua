-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:213
-- @test: MathModuleTUnitTests.FrexpWithZeroReturnsZeroMantissaAndExponent
return math.frexp(0)
