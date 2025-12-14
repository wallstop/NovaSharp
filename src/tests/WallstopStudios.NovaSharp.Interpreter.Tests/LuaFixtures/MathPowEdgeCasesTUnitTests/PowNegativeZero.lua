-- Test: math.pow behavior with negative zero (-0.0)
-- Expected: Proper IEEE 754 negative zero handling
-- Reference: Lua §6.7, IEEE 754-2008

-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: standalone-fixture
-- @test: MathPowEdgeCasesTUnitTests.PowNegativeZero
local inf = math.huge

-- Create negative zero
local negzero = -0.0

-- Verify we have negative zero (1/negzero should be -inf)
local negzero_test = 1/negzero
assert(negzero_test == -inf, "1/-0 should equal -inf, got " .. tostring(negzero_test))

-- Negative zero raised to positive odd integer
-- Per IEEE 754: -0^n = -0 for positive odd integer n
local result1 = math.pow(negzero, 1)
assert(result1 == 0, "(-0)^1 should equal 0 (or -0)")

-- Negative zero raised to positive even integer
-- Per IEEE 754: -0^n = +0 for positive even integer n
local result2 = math.pow(negzero, 2)
assert(result2 == 0, "(-0)^2 should equal 0")

-- Negative zero raised to negative power
-- Per IEEE 754: -0^n = +inf for negative even integer n
local result3 = math.pow(negzero, -1)
assert(result3 == inf or result3 == -inf, "(-0)^-1 should equal ±inf, got " .. tostring(result3))

local result4 = math.pow(negzero, -2)
assert(result4 == inf, "(-0)^-2 should equal +inf, got " .. tostring(result4))

-- Negative zero raised to 0
assert(math.pow(negzero, 0) == 1, "(-0)^0 should equal 1")

print("PASS: All negative zero pow tests passed")
