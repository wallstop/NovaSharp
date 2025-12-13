-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.PowInfinityCases
-- Test: math.pow edge cases involving infinity (math.huge)
-- Expected: IEEE 754 compliant infinity handling
-- Reference: Lua §6.7, IEEE 754-2008

local inf = math.huge
local neginf = -math.huge

-- Verify math.huge is actually infinity
assert(inf == 1/0, "math.huge should equal 1/0 (positive infinity)")
assert(neginf == -1/0, "-math.huge should equal -1/0 (negative infinity)")

-- Infinity raised to powers
assert(math.pow(inf, 0) == 1, "inf^0 should equal 1")
assert(math.pow(inf, 1) == inf, "inf^1 should equal inf")
assert(math.pow(inf, 2) == inf, "inf^2 should equal inf")
assert(math.pow(inf, -1) == 0, "inf^-1 should equal 0")
assert(math.pow(inf, -2) == 0, "inf^-2 should equal 0")

-- Negative infinity raised to powers
assert(math.pow(neginf, 0) == 1, "(-inf)^0 should equal 1")
assert(math.pow(neginf, 1) == neginf, "(-inf)^1 should equal -inf")
assert(math.pow(neginf, 2) == inf, "(-inf)^2 should equal inf (even power)")
assert(math.pow(neginf, 3) == neginf, "(-inf)^3 should equal -inf (odd power)")

-- Numbers raised to infinity
assert(math.pow(2, inf) == inf, "2^inf should equal inf (base > 1)")
assert(math.pow(2, neginf) == 0, "2^-inf should equal 0 (base > 1)")
assert(math.pow(0.5, inf) == 0, "0.5^inf should equal 0 (0 < base < 1)")
assert(math.pow(0.5, neginf) == inf, "0.5^-inf should equal inf (0 < base < 1)")

-- Infinity raised to infinity
assert(math.pow(inf, inf) == inf, "inf^inf should equal inf")
assert(math.pow(inf, neginf) == 0, "inf^-inf should equal 0")

print("PASS: All infinity pow tests passed")
