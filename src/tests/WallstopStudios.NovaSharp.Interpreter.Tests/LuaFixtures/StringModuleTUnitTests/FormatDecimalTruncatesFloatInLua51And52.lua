-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1588
-- @test: StringModuleTUnitTests.FormatDecimalTruncatesFloatInLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.format('%d', 123.456)
