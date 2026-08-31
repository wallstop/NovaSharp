-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:771
-- @test: Bit32ModuleTUnitTests.LshiftTruncatesNonIntegerShiftAmountLua52
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
local result = bit32.lshift(1, 2.5)
assert(result == 4)
print("bit32 fractional displacement parity")
return result
