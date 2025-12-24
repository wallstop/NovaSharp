-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: math.max/min with infinity values follow IEEE 754 rules

local inf = 1/0
local neginf = -1/0

-- ============================================================
-- SECTION 1: math.max with positive infinity
-- ============================================================
assert(math.max(inf, 5) == inf, "max(inf, 5) should be inf")
assert(math.max(5, inf) == inf, "max(5, inf) should be inf")
assert(math.max(inf, 0) == inf, "max(inf, 0) should be inf")
assert(math.max(0, inf) == inf, "max(0, inf) should be inf")
assert(math.max(inf, -5) == inf, "max(inf, -5) should be inf")
assert(math.max(-5, inf) == inf, "max(-5, inf) should be inf")

-- inf vs inf
assert(math.max(inf, inf) == inf, "max(inf, inf) should be inf")

-- ============================================================
-- SECTION 2: math.max with negative infinity
-- ============================================================
assert(math.max(neginf, 5) == 5, "max(-inf, 5) should be 5")
assert(math.max(5, neginf) == 5, "max(5, -inf) should be 5")
assert(math.max(neginf, 0) == 0, "max(-inf, 0) should be 0")
assert(math.max(0, neginf) == 0, "max(0, -inf) should be 0")
assert(math.max(neginf, -5) == -5, "max(-inf, -5) should be -5")
assert(math.max(-5, neginf) == -5, "max(-5, -inf) should be -5")

-- -inf vs -inf
assert(math.max(neginf, neginf) == neginf, "max(-inf, -inf) should be -inf")

-- ============================================================
-- SECTION 3: math.max with both infinities
-- ============================================================
assert(math.max(inf, neginf) == inf, "max(inf, -inf) should be inf")
assert(math.max(neginf, inf) == inf, "max(-inf, inf) should be inf")

-- ============================================================
-- SECTION 4: math.min with negative infinity
-- ============================================================
assert(math.min(neginf, 5) == neginf, "min(-inf, 5) should be -inf")
assert(math.min(5, neginf) == neginf, "min(5, -inf) should be -inf")
assert(math.min(neginf, 0) == neginf, "min(-inf, 0) should be -inf")
assert(math.min(0, neginf) == neginf, "min(0, -inf) should be -inf")
assert(math.min(neginf, -5) == neginf, "min(-inf, -5) should be -inf")
assert(math.min(-5, neginf) == neginf, "min(-5, -inf) should be -inf")

-- -inf vs -inf
assert(math.min(neginf, neginf) == neginf, "min(-inf, -inf) should be -inf")

-- ============================================================
-- SECTION 5: math.min with positive infinity
-- ============================================================
assert(math.min(inf, 5) == 5, "min(inf, 5) should be 5")
assert(math.min(5, inf) == 5, "min(5, inf) should be 5")
assert(math.min(inf, 0) == 0, "min(inf, 0) should be 0")
assert(math.min(0, inf) == 0, "min(0, inf) should be 0")
assert(math.min(inf, -5) == -5, "min(inf, -5) should be -5")
assert(math.min(-5, inf) == -5, "min(-5, inf) should be -5")

-- inf vs inf
assert(math.min(inf, inf) == inf, "min(inf, inf) should be inf")

-- ============================================================
-- SECTION 6: math.min with both infinities
-- ============================================================
assert(math.min(inf, neginf) == neginf, "min(inf, -inf) should be -inf")
assert(math.min(neginf, inf) == neginf, "min(-inf, inf) should be -inf")

-- ============================================================
-- SECTION 7: Multi-argument cases with infinity
-- ============================================================
assert(math.max(1, 2, inf, 3) == inf, "max(1, 2, inf, 3) should be inf")
assert(math.max(inf, 1, 2, 3) == inf, "max(inf, 1, 2, 3) should be inf")
assert(math.max(1, 2, 3, inf) == inf, "max(1, 2, 3, inf) should be inf")

assert(math.min(1, 2, neginf, 3) == neginf, "min(1, 2, -inf, 3) should be -inf")
assert(math.min(neginf, 1, 2, 3) == neginf, "min(-inf, 1, 2, 3) should be -inf")
assert(math.min(1, 2, 3, neginf) == neginf, "min(1, 2, 3, -inf) should be -inf")

-- Multiple infinities
assert(math.max(neginf, 5, inf) == inf, "max(-inf, 5, inf) should be inf")
assert(math.min(inf, 5, neginf) == neginf, "min(inf, 5, -inf) should be -inf")

-- ============================================================
-- SECTION 8: Infinity with negative zero
-- ============================================================
local negzero = -0.0
assert(math.max(inf, negzero) == inf, "max(inf, -0) should be inf")
assert(math.max(negzero, inf) == inf, "max(-0, inf) should be inf")
assert(math.min(neginf, negzero) == neginf, "min(-inf, -0) should be -inf")
assert(math.min(negzero, neginf) == neginf, "min(-0, -inf) should be -inf")

-- ============================================================
-- SECTION 9: Verify infinity comparisons
-- ============================================================
assert(inf > 1e308, "inf should be greater than largest finite number")
assert(neginf < -1e308, "-inf should be less than smallest finite number")
assert(inf == inf, "inf should equal itself")
assert(neginf == neginf, "-inf should equal itself")
assert(inf ~= neginf, "inf should not equal -inf")

return "pass"
