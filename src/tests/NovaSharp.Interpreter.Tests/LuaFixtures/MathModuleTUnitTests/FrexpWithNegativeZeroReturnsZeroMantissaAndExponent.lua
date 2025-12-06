-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:224
-- @test: MathModuleTUnitTests.FrexpWithNegativeZeroReturnsZeroMantissaAndExponent
return math.frexp(-0.0)
