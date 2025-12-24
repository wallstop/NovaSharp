-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:104
-- @test: StringLibTUnitTests.StringFindHonorsPlainSearchFlag
-- @compat-notes: Test targets Lua 5.1
return string.find('Hello Lua user', '%su', 1, true);
