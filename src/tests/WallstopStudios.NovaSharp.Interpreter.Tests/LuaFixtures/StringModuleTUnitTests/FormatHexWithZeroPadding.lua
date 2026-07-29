-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1216
-- @test: StringModuleTUnitTests.FormatHexWithZeroPadding
return string.format('%08x', 255)
