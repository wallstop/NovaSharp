-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:149
-- @test: MathModuleTUnitTests.TypeDistinguishesIntegersAndFloats
return math.type(5), math.type(3.14)
