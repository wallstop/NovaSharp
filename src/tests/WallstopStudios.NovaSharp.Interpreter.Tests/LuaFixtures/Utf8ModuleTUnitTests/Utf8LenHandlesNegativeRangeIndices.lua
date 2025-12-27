-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:87
-- @test: Utf8ModuleTUnitTests.Utf8LenHandlesNegativeRangeIndices
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: utf8 library
local fromEnd = utf8.len(word, -3, -1)
                local clamped = utf8.len(word, 0, 2)
                return fromEnd, clamped
