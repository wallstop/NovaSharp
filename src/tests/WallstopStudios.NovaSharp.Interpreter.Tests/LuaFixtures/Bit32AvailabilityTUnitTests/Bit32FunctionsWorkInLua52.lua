-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:144
-- @test: Bit32AvailabilityTUnitTests.Bit32FunctionsWorkInLua52
-- @compat-notes: bit32 library only available by default in Lua 5.2 (not bundled in 5.3+)
local band = bit32.band(0xFF, 0x0F)
local bor = bit32.bor(0xF0, 0x0F)
local bxor = bit32.bxor(0xFF, 0x0F)
return band, bor, bxor