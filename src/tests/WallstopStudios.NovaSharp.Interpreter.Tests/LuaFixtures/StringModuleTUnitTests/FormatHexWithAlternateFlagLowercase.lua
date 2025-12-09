-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1041
-- @test: StringModuleTUnitTests.FormatHexWithAlternateFlagLowercase
return string.format('%#x', 255)
