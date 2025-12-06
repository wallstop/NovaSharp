-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:534
-- @test: StringModuleTUnitTests.FormatOctalWithLeftAlignAndAlternate
return string.format('%-#8o', 8)
