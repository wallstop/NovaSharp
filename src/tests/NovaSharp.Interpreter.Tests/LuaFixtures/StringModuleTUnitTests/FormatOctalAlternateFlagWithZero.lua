-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:498
-- @test: StringModuleTUnitTests.FormatOctalAlternateFlagWithZero
return string.format('%#o', 0)
