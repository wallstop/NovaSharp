-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:677
-- @test: StringModuleTUnitTests.FormatHexLeftAlignWithAlternate
return string.format('%-#8x', 255)
