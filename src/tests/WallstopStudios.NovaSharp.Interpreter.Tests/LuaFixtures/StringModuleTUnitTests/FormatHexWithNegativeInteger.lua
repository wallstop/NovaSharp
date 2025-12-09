-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1410
-- @test: StringModuleTUnitTests.FormatHexWithNegativeInteger
return string.format('%x', -1)
