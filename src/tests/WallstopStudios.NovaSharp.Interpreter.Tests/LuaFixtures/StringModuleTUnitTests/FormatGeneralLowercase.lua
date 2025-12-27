-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1405
-- @test: StringModuleTUnitTests.FormatGeneralLowercase
return string.format('%g', 0.0001234)
