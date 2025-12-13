-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.PowZeroCases
-- Test: math.pow edge cases involving zero
-- Expected: IEEE 754 compliant results
-- Reference: Lua §6.7, IEEE 754-2008

-- 0^0 is defined as 1 in Lua (per C99 pow specification)
assert(math.pow(0, 0) == 1, "0^0 should equal 1")
assert(0^0 == 1, "0^0 operator should equal 1")

-- 0 raised to positive powers
assert(math.pow(0, 1) == 0, "0^1 should equal 0")
assert(math.pow(0, 2) == 0, "0^2 should equal 0")
assert(math.pow(0, 100) == 0, "0^100 should equal 0")
assert(math.pow(0, 0.5) == 0, "0^0.5 should equal 0")

-- 0 raised to negative powers (division by zero -> infinity)
local result = math.pow(0, -1)
assert(result == math.huge, "0^-1 should equal infinity, got " .. tostring(result))
assert(math.pow(0, -2) == math.huge, "0^-2 should equal infinity")
assert(math.pow(0, -0.5) == math.huge, "0^-0.5 should equal infinity")

-- Anything (except 0 and NaN) raised to 0 equals 1
assert(math.pow(1, 0) == 1, "1^0 should equal 1")
assert(math.pow(-1, 0) == 1, "(-1)^0 should equal 1")
assert(math.pow(2, 0) == 1, "2^0 should equal 1")
assert(math.pow(-2, 0) == 1, "(-2)^0 should equal 1")
assert(math.pow(100, 0) == 1, "100^0 should equal 1")
assert(math.pow(-100, 0) == 1, "(-100)^0 should equal 1")
assert(math.pow(0.5, 0) == 1, "0.5^0 should equal 1")
assert(math.pow(math.huge, 0) == 1, "inf^0 should equal 1")

print("PASS: All zero pow tests passed")
