-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:88
-- @test: StringPackModuleTUnitTests.PackUnpackDouble
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('d', 3.14159265358979)
                local unpacked = string.unpack('d', packed)
                return unpacked
