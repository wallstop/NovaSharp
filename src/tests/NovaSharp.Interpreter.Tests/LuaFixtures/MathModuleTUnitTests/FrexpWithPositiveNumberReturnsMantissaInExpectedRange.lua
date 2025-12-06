-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:277
-- @test: MathModuleTUnitTests.FrexpWithPositiveNumberReturnsMantissaInExpectedRange
return math.frexp(16)
