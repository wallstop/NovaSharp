-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2845
-- @test: StringModuleTUnitTests.FormatWidthSpecifiersString
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.format('{format}', '{value}')
