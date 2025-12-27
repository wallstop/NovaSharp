-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:319
-- @test: StringPackModuleTUnitTests.PackLuaInteger
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('j', 9223372036854775807)
                local unpacked = string.unpack('j', packed)
                return unpacked
