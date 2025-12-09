-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:168
-- @test: MathModuleTUnitTests.ToIntegerReturnsNilWhenValueNotIntegral
return math.tointeger(3.5)
