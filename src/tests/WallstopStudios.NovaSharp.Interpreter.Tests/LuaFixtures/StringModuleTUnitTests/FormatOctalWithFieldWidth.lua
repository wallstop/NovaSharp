-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:943
-- @test: StringModuleTUnitTests.FormatOctalWithFieldWidth
return string.format('%8o', 8)
