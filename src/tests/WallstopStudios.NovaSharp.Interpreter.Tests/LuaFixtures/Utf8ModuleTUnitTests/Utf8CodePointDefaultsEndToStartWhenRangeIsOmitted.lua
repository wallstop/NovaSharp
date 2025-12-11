-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:123
-- @test: Utf8ModuleTUnitTests.Utf8CodePointDefaultsEndToStartWhenRangeIsOmitted
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local results = { utf8.codepoint(word, 2) }
                return #results, results[1]
