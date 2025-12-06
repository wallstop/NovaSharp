-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:525
-- @test: StringModuleTUnitTests.FormatOctalWithLeftAlign
return string.format('%-8o', 8)
