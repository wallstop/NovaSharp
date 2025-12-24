-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: math.min with NaN uses standard comparison (NaN comparisons are always false)
--                This means: if NaN is FIRST arg, result is NaN (first wins when comparison fails)
--                            if NaN is LATER arg, it's skipped (non-NaN values win)

local nan = 0/0
local inf = 1/0

-- Helper to check NaN (NaN is not equal to itself)
local function isNaN(x)
    return x ~= x
end

-- ============================================================
-- SECTION 1: NaN as first argument - result is NaN
-- ============================================================
-- When NaN is first, the comparison (nan < x) returns false, so first value (nan) is kept
local r1 = math.min(nan, 5)
assert(isNaN(r1), "min(nan, 5) should be NaN - NaN as first arg wins")

local r2 = math.min(nan, -5)
assert(isNaN(r2), "min(nan, -5) should be NaN - NaN as first arg wins")

local r3 = math.min(nan, 0)
assert(isNaN(r3), "min(nan, 0) should be NaN - NaN as first arg wins")

-- ============================================================
-- SECTION 2: NaN as second argument - non-NaN wins
-- ============================================================
-- When NaN is second, comparison (5 < nan) returns false, so first value (5) is kept
local r4 = math.min(5, nan)
assert(r4 == 5, "min(5, nan) should be 5 - non-NaN as first arg wins")

local r5 = math.min(-5, nan)
assert(r5 == -5, "min(-5, nan) should be -5 - non-NaN as first arg wins")

local r6 = math.min(0, nan)
assert(r6 == 0, "min(0, nan) should be 0 - non-NaN as first arg wins")

-- ============================================================
-- SECTION 3: Both arguments are NaN - result is NaN
-- ============================================================
local r7 = math.min(nan, nan)
assert(isNaN(r7), "min(nan, nan) should be NaN")

-- ============================================================
-- SECTION 4: Multi-argument cases with NaN
-- ============================================================
-- NaN as first argument - result is NaN (stays NaN throughout)
local r8 = math.min(nan, 1, 2)
assert(isNaN(r8), "min(nan, 1, 2) should be NaN - NaN as first arg propagates")

local r9 = math.min(nan, 1, 2, 3, 4, 5)
assert(isNaN(r9), "min(nan, 1, 2, 3, 4, 5) should be NaN")

-- NaN in middle - gets skipped, min of non-NaN values before it wins
local r10 = math.min(3, nan, 1)
assert(r10 == 1, "min(3, nan, 1) should be 1 - NaN in middle skipped")

local r11 = math.min(1, nan, 3)
assert(r11 == 1, "min(1, nan, 3) should be 1 - NaN in middle skipped, 1 < 3")

-- NaN as last argument - gets skipped
local r12 = math.min(2, 1, nan)
assert(r12 == 1, "min(2, 1, nan) should be 1 - NaN at end skipped")

local r13 = math.min(1, 3, 5, nan)
assert(r13 == 1, "min(1, 3, 5, nan) should be 1 - NaN at end skipped")

-- Multiple NaN values interspersed
local r14 = math.min(5, nan, 3, nan, 1)
assert(r14 == 1, "min(5, nan, 3, nan, 1) should be 1 - multiple NaN skipped")

local r15 = math.min(1, nan, 3, nan, 5)
assert(r15 == 1, "min(1, nan, 3, nan, 5) should be 1")

-- ============================================================
-- SECTION 5: NaN with infinity
-- ============================================================
-- -inf as first arg, nan as second - -inf wins (nan skipped)
local r16 = math.min(-inf, nan)
assert(r16 == -inf, "min(-inf, nan) should be -inf - -inf wins over NaN")

-- nan as first arg, -inf as second - nan wins (comparison fails)
local r17 = math.min(nan, -inf)
assert(isNaN(r17), "min(nan, -inf) should be NaN - NaN as first arg wins")

-- Positive infinity cases
local r18 = math.min(inf, nan)
assert(r18 == inf, "min(inf, nan) should be inf - inf wins over NaN")

local r19 = math.min(nan, inf)
assert(isNaN(r19), "min(nan, inf) should be NaN - NaN as first arg wins")

-- Multi-arg with inf and nan
local r20 = math.min(5, -inf, nan)
assert(r20 == -inf, "min(5, -inf, nan) should be -inf")

local r21 = math.min(nan, -inf, 5)
assert(isNaN(r21), "min(nan, -inf, 5) should be NaN - NaN first")

-- ============================================================
-- SECTION 6: NaN with negative zero
-- ============================================================
local negzero = -0.0
local r22 = math.min(nan, negzero)
assert(isNaN(r22), "min(nan, -0) should be NaN - NaN as first arg")

local r23 = math.min(negzero, nan)
assert(r23 == 0, "min(-0, nan) should be 0 (or -0) - non-NaN wins")

return "pass"
