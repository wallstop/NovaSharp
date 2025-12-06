-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:807
-- @test: StringModuleTUnitTests.FormatGeneralUppercase
return string.format('%G', 0.0001234)
