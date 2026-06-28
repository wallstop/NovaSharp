-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\Bit32AvailabilityTUnitTests.cs:144
-- @test: Bit32AvailabilityTUnitTests.Bit32FunctionsWorkInLua52
-- Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
local band = bit32.band(0xFF, 0x0F)
                local bor = bit32.bor(0xF0, 0x0F)
                local bxor = bit32.bxor(0xFF, 0x0F)
                return band, bor, bxor
