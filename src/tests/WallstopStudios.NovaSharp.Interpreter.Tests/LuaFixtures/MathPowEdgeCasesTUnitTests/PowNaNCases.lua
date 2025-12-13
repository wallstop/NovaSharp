-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.PowNaNCases
-- Test: math.pow edge cases involving NaN
-- Expected: IEEE 754 compliant NaN handling
-- Reference: Lua §6.7, IEEE 754-2008, C99 pow specification

local nan = 0/0

-- Helper to check if a value is NaN
local function isnan(x)
  return x ~= x
end

-- NaN raised to 0 equals 1 (C99 pow special case: pow(x, ±0) = 1 for any x, even NaN)
assert(math.pow(nan, 0) == 1, "nan^0 should equal 1")

-- NaN raised to any other power is NaN
assert(isnan(math.pow(nan, 1)), "nan^1 should be nan")
assert(isnan(math.pow(nan, 2)), "nan^2 should be nan")
assert(isnan(math.pow(nan, -1)), "nan^-1 should be nan")
assert(isnan(math.pow(nan, 0.5)), "nan^0.5 should be nan")

-- Any number (except 1) raised to NaN is NaN
assert(isnan(math.pow(0, nan)), "0^nan should be nan")
assert(isnan(math.pow(2, nan)), "2^nan should be nan")
assert(isnan(math.pow(-2, nan)), "(-2)^nan should be nan")
assert(isnan(math.pow(0.5, nan)), "0.5^nan should be nan")
assert(isnan(math.pow(math.huge, nan)), "inf^nan should be nan")

-- 1 raised to NaN equals 1 (C99 pow special case)
assert(math.pow(1, nan) == 1, "1^nan should equal 1")

-- NaN raised to NaN is NaN
assert(isnan(math.pow(nan, nan)), "nan^nan should be nan")

print("PASS: All NaN pow tests passed")
