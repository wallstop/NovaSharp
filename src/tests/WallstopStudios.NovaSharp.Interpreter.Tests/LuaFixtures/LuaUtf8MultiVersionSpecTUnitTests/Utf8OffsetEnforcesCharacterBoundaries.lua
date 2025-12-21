-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaUtf8MultiVersionSpecTUnitTests.cs:67
-- @test: LuaUtf8MultiVersionSpecTUnitTests.Utf8OffsetEnforcesCharacterBoundaries
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: utf8 library
local forward2 = utf8.offset(word, 2)
                local back1 = utf8.offset(word, -1)
                local align = utf8.offset(word, 0, 3)
                return forward2, back1, align
