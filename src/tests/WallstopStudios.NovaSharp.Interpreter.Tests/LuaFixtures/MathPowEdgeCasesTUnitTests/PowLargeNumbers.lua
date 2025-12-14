-- Test: math.pow with very large numbers (overflow to infinity)
-- Expected: Proper overflow handling per IEEE 754
-- Reference: Lua §6.7, IEEE 754-2008

-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: standalone-fixture
-- @test: MathPowEdgeCasesTUnitTests.PowLargeNumbers
local inf = math.huge

-- Large exponents that stay within double range
local result = math.pow(10, 308)
assert(result > 0 and result < inf, "10^308 should be a large finite number")

-- Large exponents that overflow to infinity
assert(math.pow(10, 309) == inf, "10^309 should overflow to infinity")
assert(math.pow(10, 1000) == inf, "10^1000 should overflow to infinity")
assert(math.pow(2, 1024) == inf, "2^1024 should overflow to infinity")

-- Underflow to zero
local tiny = math.pow(10, -308)
assert(tiny > 0, "10^-308 should be a small positive number")

assert(math.pow(10, -324) == 0, "10^-324 should underflow to zero")
assert(math.pow(10, -400) == 0, "10^-400 should underflow to zero")

-- Large base values
assert(math.pow(inf, 0) == 1, "inf^0 should equal 1")
assert(math.pow(inf, 0.5) == inf, "inf^0.5 should equal inf")

-- Precision test for moderately large values
local val = math.pow(2, 53)
assert(val == 9007199254740992, "2^53 should equal 9007199254740992")

print("PASS: All large number pow tests passed")
