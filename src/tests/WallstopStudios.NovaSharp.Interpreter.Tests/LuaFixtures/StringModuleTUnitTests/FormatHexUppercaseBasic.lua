-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1176
-- @test: StringModuleTUnitTests.FormatHexUppercaseBasic
return string.format('%X', 255)
