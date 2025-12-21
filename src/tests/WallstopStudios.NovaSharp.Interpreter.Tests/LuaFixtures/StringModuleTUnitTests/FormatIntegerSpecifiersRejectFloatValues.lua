-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2293
-- @test: StringModuleTUnitTests.FormatIntegerSpecifiersRejectFloatValues
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.4+
return string.format('{specifier}', 123.456)
