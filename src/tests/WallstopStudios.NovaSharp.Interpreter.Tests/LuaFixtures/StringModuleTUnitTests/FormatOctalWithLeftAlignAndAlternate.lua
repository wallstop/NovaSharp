-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:970
-- @test: StringModuleTUnitTests.FormatOctalWithLeftAlignAndAlternate
return string.format('%-#8o', 8)
