-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:86
-- @test: MathModuleTUnitTests.LdexpCombinesMantissaAndExponent
return math.ldexp(0.5, 3)
