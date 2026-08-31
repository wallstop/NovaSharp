-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:753
-- @test: Bit32ModuleTUnitTests.BandAcceptsIntegralFloatLua52
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
local result = bit32.band(5.0, 3.0)
assert(result == 1)
print("bit32 integral-float parity")
return result
