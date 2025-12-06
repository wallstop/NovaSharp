-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:860
-- @test: StringModuleTUnitTests.FormatStringWithLeftAlign
return string.format('%-10s', 'Hello')
