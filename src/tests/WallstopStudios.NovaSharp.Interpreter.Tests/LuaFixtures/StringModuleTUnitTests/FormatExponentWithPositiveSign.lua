-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:816
-- @test: StringModuleTUnitTests.FormatExponentWithPositiveSign
return string.format('%+e', 12345.6)
