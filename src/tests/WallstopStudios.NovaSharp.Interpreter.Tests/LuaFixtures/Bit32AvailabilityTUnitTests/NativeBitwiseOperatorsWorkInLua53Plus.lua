-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:171
-- @test: Bit32AvailabilityTUnitTests.NativeBitwiseOperatorsWorkInLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise AND; Lua 5.3+: bitwise OR; Lua 5.3+: bitwise XOR/NOT; Lua 5.3+: bit shift
local a = 0xFF
                local b = 0x0F
                local band = a & b
                local bor = a | b
                local bxor = a ~ b
                local bnot = ~0
                local lshift = 1 << 4
                local rshift = 16 >> 2
                return band, bor, bxor, bnot, lshift, rshift
