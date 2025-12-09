-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:293
-- @test: Utf8ModuleTUnitTests.Utf8OffsetNormalizesNegativeAndZeroBoundaries
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local fromEnd = utf8.offset('abcd', 1, -1)
                local clamped = utf8.offset('abcd', 1, 0)
                return fromEnd, clamped
