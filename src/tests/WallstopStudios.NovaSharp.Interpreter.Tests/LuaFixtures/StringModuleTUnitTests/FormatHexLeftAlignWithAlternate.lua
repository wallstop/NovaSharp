-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1113
-- @test: StringModuleTUnitTests.FormatHexLeftAlignWithAlternate
return string.format('%-#8x', 255)
