-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1260
-- @test: StringModuleTUnitTests.FormatHexZeroPaddingWithAlternateUppercase
return string.format('%#08X', 255)
