-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:668
-- @test: StringModuleTUnitTests.FormatHexZeroPaddingWithAlternateUppercase
return string.format('%#08X', 255)
