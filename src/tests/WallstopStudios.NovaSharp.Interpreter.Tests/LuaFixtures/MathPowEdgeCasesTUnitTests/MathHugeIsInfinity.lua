-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.MathHugeIsInfinity
-- Test: Verify math.huge is IEEE 754 positive infinity
-- Expected: math.huge equals 1/0 and behaves as infinity
-- Reference: Lua §6.7 - "math.huge: The float value HUGE_VAL, a value greater than any other numeric value"

local inf = math.huge

-- math.huge must equal 1/0 (IEEE 754 positive infinity)
assert(inf == 1/0, "math.huge should equal 1/0 (positive infinity)")
assert(-inf == -1/0, "-math.huge should equal -1/0 (negative infinity)")

-- Infinity arithmetic properties
assert(inf + 1 == inf, "inf + 1 should equal inf")
assert(inf * 2 == inf, "inf * 2 should equal inf")
assert(inf / 2 == inf, "inf / 2 should equal inf")
assert(1 / inf == 0, "1 / inf should equal 0")

-- Infinity comparisons
assert(inf > 1e308, "inf should be greater than 1e308")
assert(-inf < -1e308, "-inf should be less than -1e308")
assert(inf == inf, "inf should equal itself")

-- Infinity is not NaN
assert(inf == inf, "inf should equal itself (not NaN)")
assert(not (inf ~= inf), "inf ~= inf should be false")

-- Type check (Lua 5.3+)
if math.type then
  assert(math.type(inf) == "float", "math.huge should be float type")
end

print("PASS: math.huge is correctly defined as infinity")
