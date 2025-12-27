-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:594
-- @test: StringModuleTUnitTests.SubTruncatesFloatIndicesLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.sub('Lua', 1.5, 3)
