-- Tests that math.random(n) accepts float values that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.7: 5.0 has integer representation

-- This should succeed - 5.0 is a float but has integer representation
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomIntegerFloat_53plus
local result = math.random(5.0)
assert(result >= 1 and result <= 5, "Result should be in [1, 5] range")
print("PASS: math.random(5.0) accepted")
