-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:789
-- @test: StringModuleTUnitTests.FormatExponentUppercase
return string.format('%E', 12345.6)
