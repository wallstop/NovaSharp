-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:79
-- @test: StringModuleTUnitTests.CharAcceptsIntegralFloatValues
return string.char(65.0)
