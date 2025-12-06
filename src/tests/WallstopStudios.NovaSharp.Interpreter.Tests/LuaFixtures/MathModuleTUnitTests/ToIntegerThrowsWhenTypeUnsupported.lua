-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:191
-- @test: MathModuleTUnitTests.ToIntegerThrowsWhenTypeUnsupported
return math.tointeger(true)
