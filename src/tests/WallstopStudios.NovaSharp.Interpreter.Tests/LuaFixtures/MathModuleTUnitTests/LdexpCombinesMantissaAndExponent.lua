-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:115
-- @test: MathModuleTUnitTests.LdexpCombinesMantissaAndExponent
return math.ldexp(0.5, 3)
