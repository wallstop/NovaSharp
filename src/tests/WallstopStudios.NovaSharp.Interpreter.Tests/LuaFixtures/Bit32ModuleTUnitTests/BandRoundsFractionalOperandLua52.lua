-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:735
-- @test: Bit32ModuleTUnitTests.BandRoundsFractionalOperandLua52
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
local result = bit32.band(5.7, 3)
assert(result == 2)
print("bit32 fractional normalization parity")
return result
