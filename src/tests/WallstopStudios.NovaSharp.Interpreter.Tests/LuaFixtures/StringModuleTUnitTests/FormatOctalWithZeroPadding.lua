-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:516
-- @test: StringModuleTUnitTests.FormatOctalWithZeroPadding
return string.format('%08o', 8)
