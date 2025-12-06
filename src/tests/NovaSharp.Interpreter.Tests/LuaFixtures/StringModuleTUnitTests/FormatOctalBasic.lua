-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:480
-- @test: StringModuleTUnitTests.FormatOctalBasic
return string.format('%o', 8)
