-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1243
-- @test: StringModuleTUnitTests.FormatGeneralUppercase
return string.format('%G', 0.0001234)
