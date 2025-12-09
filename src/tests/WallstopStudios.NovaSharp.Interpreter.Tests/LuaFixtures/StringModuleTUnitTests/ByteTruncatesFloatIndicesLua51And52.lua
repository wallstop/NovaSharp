-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:239
-- @test: StringModuleTUnitTests.ByteTruncatesFloatIndicesLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.byte('Lua', 1.5)
