-- Tests that math.fmod(x, 0) returns NaN in Lua 5.1/5.2
-- Lua 5.3+ changed this to throw an error instead

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.FmodZeroDivisorReturnsNanLua51And52
-- @compat-notes: Lua 5.1/5.2 return NaN for zero divisor; Lua 5.3+ throws error

-- Test positive dividend / zero
local result1 = math.fmod(5, 0)
local isNan1 = result1 ~= result1
print("fmod(5, 0) is NaN:", isNan1)

-- Test negative dividend / zero
local result2 = math.fmod(-5, 0)
local isNan2 = result2 ~= result2
print("fmod(-5, 0) is NaN:", isNan2)

-- Test float dividend / zero
local result3 = math.fmod(5.5, 0)
local isNan3 = result3 ~= result3
print("fmod(5.5, 0) is NaN:", isNan3)

-- Test zero dividend / zero
local result4 = math.fmod(0, 0)
local isNan4 = result4 ~= result4
print("fmod(0, 0) is NaN:", isNan4)

-- All should be NaN
assert(isNan1, "fmod(5, 0) should be NaN")
assert(isNan2, "fmod(-5, 0) should be NaN")
assert(isNan3, "fmod(5.5, 0) should be NaN")
assert(isNan4, "fmod(0, 0) should be NaN")

print("All zero divisor tests passed - returned NaN as expected")
