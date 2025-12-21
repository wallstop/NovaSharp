-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:886
-- @test: StringModuleTUnitTests.RepTruncatesFloatCountLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.rep('a', 2.5)
