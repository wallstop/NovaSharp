-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:65
-- @test: Utf8ModuleTUnitTests.Utf8LenHandlesNegativeRangeIndices
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local fromEnd = utf8.len(word, -3, -1)
                local clamped = utf8.len(word, 0, 2)
                return fromEnd, clamped
