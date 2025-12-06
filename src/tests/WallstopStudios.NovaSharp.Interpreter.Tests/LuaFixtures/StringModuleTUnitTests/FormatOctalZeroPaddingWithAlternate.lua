-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:543
-- @test: StringModuleTUnitTests.FormatOctalZeroPaddingWithAlternate
return string.format('%#08o', 8)
