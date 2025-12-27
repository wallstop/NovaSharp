-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:104
-- @test: StringPackModuleTUnitTests.PackUnpackFloat
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+); Lua 5.3+: string.unpack (5.3+)
local packed = string.pack('f', 3.14)
                local unpacked = string.unpack('f', packed)
                return math.abs(unpacked - 3.14) < 0.001
