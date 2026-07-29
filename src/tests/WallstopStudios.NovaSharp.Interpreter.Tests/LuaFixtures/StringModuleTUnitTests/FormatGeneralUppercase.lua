-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1415
-- @test: StringModuleTUnitTests.FormatGeneralUppercase
return string.format('%G', 0.0001234)
