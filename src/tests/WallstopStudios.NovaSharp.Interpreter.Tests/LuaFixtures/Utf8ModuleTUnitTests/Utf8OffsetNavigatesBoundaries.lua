-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:257
-- @test: Utf8ModuleTUnitTests.Utf8OffsetNavigatesBoundaries
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local forward1 = utf8.offset(word, 1)
                local forward2 = utf8.offset(word, 2)
                local back1 = utf8.offset(word, -1)
                local back2 = utf8.offset(word, -2)
                local align = utf8.offset(word, 0, 3)
                return forward1, forward2, back1, back2, align
