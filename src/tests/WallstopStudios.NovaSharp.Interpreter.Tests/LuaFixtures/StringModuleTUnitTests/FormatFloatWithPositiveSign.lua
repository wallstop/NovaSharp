-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1361
-- @test: StringModuleTUnitTests.FormatFloatWithPositiveSign
return string.format('%+f', 3.14)
