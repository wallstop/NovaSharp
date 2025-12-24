-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.MaxMinMixedNan
-- @compat-notes: NaN comparison behavior in Lua - NaN comparisons are always false
-- When NaN is not the first arg, it's effectively skipped since comparisons with NaN are false
-- When NaN is the first arg, it becomes the initial value and may not be replaced

local nan = 0/0
local function isnan(x)
    return x ~= x
end

-- Test: math.max with NaN in middle of args
-- math.max(1, 2, nan, 3): starts at 1, 2>1 so becomes 2, nan>2 is false, 3>2 so becomes 3
local result1 = math.max(1, 2, nan, 3)
assert(result1 == 3, "math.max(1, 2, nan, 3) should return 3, got " .. tostring(result1))

-- Test: math.min with NaN in middle of args
-- math.min(3, 2, nan, 1): starts at 3, 2<3 so becomes 2, nan<2 is false, 1<2 so becomes 1
local result2 = math.min(3, 2, nan, 1)
assert(result2 == 1, "math.min(3, 2, nan, 1) should return 1, got " .. tostring(result2))

-- Test: math.max with NaN at start - NaN becomes initial max and comparisons fail
local result3 = math.max(nan, 1, 2, 3)
assert(isnan(result3), "math.max(nan, 1, 2, 3) should return NaN, got " .. tostring(result3))

-- Test: math.min with NaN at start - NaN becomes initial min and comparisons fail
local result4 = math.min(nan, 3, 2, 1)
assert(isnan(result4), "math.min(nan, 3, 2, 1) should return NaN, got " .. tostring(result4))

-- Test: math.max with NaN at end
-- math.max(1, 2, 3, nan): starts at 1, becomes 2, becomes 3, nan>3 is false, stays 3
local result5 = math.max(1, 2, 3, nan)
assert(result5 == 3, "math.max(1, 2, 3, nan) should return 3, got " .. tostring(result5))

-- Test: math.min with NaN at end
-- math.min(3, 2, 1, nan): starts at 3, becomes 2, becomes 1, nan<1 is false, stays 1
local result6 = math.min(3, 2, 1, nan)
assert(result6 == 1, "math.min(3, 2, 1, nan) should return 1, got " .. tostring(result6))

-- Test: math.max with multiple NaNs interspersed (NaN first means result is NaN)
local result7 = math.max(nan, 1, nan, 5, nan, 3, nan)
assert(isnan(result7), "math.max(nan, 1, nan, 5, nan, 3, nan) should return NaN, got " .. tostring(result7))

-- Test: math.min with multiple NaNs interspersed (NaN first means result is NaN)
local result8 = math.min(nan, 5, nan, 1, nan, 3, nan)
assert(isnan(result8), "math.min(nan, 5, nan, 1, nan, 3, nan) should return NaN, got " .. tostring(result8))

-- Test: math.max with real first, then NaNs interspersed
-- math.max(1, nan, 5, nan, 3): starts at 1, nan>1 false, 5>1 yes becomes 5, nan>5 false, 3>5 false
local result9 = math.max(1, nan, 5, nan, 3)
assert(result9 == 5, "math.max(1, nan, 5, nan, 3) should return 5, got " .. tostring(result9))

-- Test: math.min with real first, then NaNs interspersed
-- math.min(5, nan, 1, nan, 3): starts at 5, nan<5 false, 1<5 yes becomes 1, nan<1 false, 3<1 false
local result10 = math.min(5, nan, 1, nan, 3)
assert(result10 == 1, "math.min(5, nan, 1, nan, 3) should return 1, got " .. tostring(result10))

-- Test: math.max with negative numbers and NaN (NaN not first)
local result11 = math.max(-10, nan, -5, nan, -1)
assert(result11 == -1, "math.max(-10, nan, -5, nan, -1) should return -1, got " .. tostring(result11))

-- Test: math.min with negative numbers and NaN (NaN not first)
local result12 = math.min(-1, nan, -5, nan, -10)
assert(result12 == -10, "math.min(-1, nan, -5, nan, -10) should return -10, got " .. tostring(result12))

-- Test: math.max with infinity, negative infinity and NaN mixed (real first)
local result13 = math.max(-math.huge, nan, 0, nan, math.huge)
assert(result13 == math.huge, "math.max(-inf, nan, 0, nan, inf) should return inf, got " .. tostring(result13))

-- Test: math.min with infinity, negative infinity and NaN mixed (real first)
local result14 = math.min(math.huge, nan, 0, nan, -math.huge)
assert(result14 == -math.huge, "math.min(inf, nan, 0, nan, -inf) should return -inf, got " .. tostring(result14))

-- All tests passed
print("MaxMinMixedNan: all tests passed")
return true
