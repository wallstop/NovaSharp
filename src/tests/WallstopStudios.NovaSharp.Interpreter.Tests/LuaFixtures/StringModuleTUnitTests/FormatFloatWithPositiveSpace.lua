-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1371
-- @test: StringModuleTUnitTests.FormatFloatWithPositiveSpace
return string.format('% f', 3.14)
