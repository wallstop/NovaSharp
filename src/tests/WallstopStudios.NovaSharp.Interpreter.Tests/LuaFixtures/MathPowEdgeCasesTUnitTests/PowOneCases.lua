-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.PowOneCases
-- Test: math.pow edge cases involving one
-- Expected: 1 raised to any power equals 1
-- Reference: Lua §6.7, IEEE 754-2008

-- 1 raised to any finite power equals 1
assert(math.pow(1, 0) == 1, "1^0 should equal 1")
assert(math.pow(1, 1) == 1, "1^1 should equal 1")
assert(math.pow(1, 2) == 1, "1^2 should equal 1")
assert(math.pow(1, 100) == 1, "1^100 should equal 1")
assert(math.pow(1, -1) == 1, "1^-1 should equal 1")
assert(math.pow(1, -100) == 1, "1^-100 should equal 1")
assert(math.pow(1, 0.5) == 1, "1^0.5 should equal 1")
assert(math.pow(1, -0.5) == 1, "1^-0.5 should equal 1")

-- 1 raised to infinity equals 1 (C99 pow special case)
assert(math.pow(1, math.huge) == 1, "1^inf should equal 1")
assert(math.pow(1, -math.huge) == 1, "1^-inf should equal 1")

-- 1 raised to NaN equals 1 (C99 pow special case)
local nan = 0/0
assert(math.pow(1, nan) == 1, "1^nan should equal 1")

-- Anything raised to 1 equals itself
assert(math.pow(0, 1) == 0, "0^1 should equal 0")
assert(math.pow(2, 1) == 2, "2^1 should equal 2")
assert(math.pow(-5, 1) == -5, "(-5)^1 should equal -5")
assert(math.pow(3.14, 1) == 3.14, "3.14^1 should equal 3.14")

print("PASS: All one pow tests passed")
