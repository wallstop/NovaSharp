-- Test: Negative base with fractional exponent produces NaN
-- Expected: NaN result (complex number in real domain)
-- Reference: Lua §6.7, IEEE 754-2008

-- Helper to check if a value is NaN
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: standalone-fixture
-- @test: MathPowEdgeCasesTUnitTests.PowNegativeBaseFractionalExponent
local function isnan(x)
  return x ~= x
end

-- Negative base with fractional exponent produces NaN in real arithmetic
-- (mathematically these are complex numbers, but Lua uses real arithmetic)
assert(isnan(math.pow(-2, 0.5)), "(-2)^0.5 should be nan (sqrt of negative)")
assert(isnan(math.pow(-8, 1/3)), "(-8)^(1/3) should be nan (cube root of negative)")
assert(isnan(math.pow(-1, 0.5)), "(-1)^0.5 should be nan (sqrt of -1)")
assert(isnan(math.pow(-4, 0.25)), "(-4)^0.25 should be nan (fourth root of negative)")

-- Verify operator behaves the same
assert(isnan((-2)^0.5), "(-2)^0.5 operator should be nan")
assert(isnan((-8)^(1/3)), "(-8)^(1/3) operator should be nan")

-- Negative base with integer exponent is fine
assert(math.pow(-2, 2) == 4, "(-2)^2 should equal 4")
assert(math.pow(-2, 3) == -8, "(-2)^3 should equal -8")

-- Edge case: -1 raised to integer powers
assert(math.pow(-1, 2) == 1, "(-1)^2 should equal 1")
assert(math.pow(-1, 3) == -1, "(-1)^3 should equal -1")
assert(math.pow(-1, 100) == 1, "(-1)^100 should equal 1 (even)")
assert(math.pow(-1, 101) == -1, "(-1)^101 should equal -1 (odd)")

print("PASS: All negative base fractional exponent tests passed")
