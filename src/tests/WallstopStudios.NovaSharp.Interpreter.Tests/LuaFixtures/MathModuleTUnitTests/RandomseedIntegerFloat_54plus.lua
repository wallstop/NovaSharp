-- Tests that math.randomseed(x) accepts float values that have integer representation in Lua 5.4+
-- Per Lua 5.4 manual §6.7: 42.0 has integer representation

-- This should succeed - 42.0 is a float but has integer representation
-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomseedIntegerFloat_54plus
math.randomseed(42.0)
local r1 = math.random()
math.randomseed(42.0)
local r2 = math.random()
assert(r1 == r2, "Seeding with same value should be deterministic")
print("PASS: math.randomseed(42.0) accepted")
