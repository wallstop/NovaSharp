-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: math.max with NaN uses standard comparison (NaN comparisons are always false)
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
-- When NaN is first, the comparison (nan > x) returns false, so first value (nan) is kept
local r1 = math.max(nan, 5)
assert(isNaN(r1), "max(nan, 5) should be NaN - NaN as first arg wins")

local r2 = math.max(nan, -5)
assert(isNaN(r2), "max(nan, -5) should be NaN - NaN as first arg wins")

local r3 = math.max(nan, 0)
assert(isNaN(r3), "max(nan, 0) should be NaN - NaN as first arg wins")

-- ============================================================
-- SECTION 2: NaN as second argument - non-NaN wins
-- ============================================================
-- When NaN is second, comparison (5 > nan) returns false, so first value (5) is kept
local r4 = math.max(5, nan)
assert(r4 == 5, "max(5, nan) should be 5 - non-NaN as first arg wins")

local r5 = math.max(-5, nan)
assert(r5 == -5, "max(-5, nan) should be -5 - non-NaN as first arg wins")

local r6 = math.max(0, nan)
assert(r6 == 0, "max(0, nan) should be 0 - non-NaN as first arg wins")

-- ============================================================
-- SECTION 3: Both arguments are NaN - result is NaN
-- ============================================================
local r7 = math.max(nan, nan)
assert(isNaN(r7), "max(nan, nan) should be NaN")

-- ============================================================
-- SECTION 4: Multi-argument cases with NaN
-- ============================================================
-- NaN as first argument - result is NaN (stays NaN throughout)
local r8 = math.max(nan, 1, 2)
assert(isNaN(r8), "max(nan, 1, 2) should be NaN - NaN as first arg propagates")

local r9 = math.max(nan, 1, 2, 3, 4, 5)
assert(isNaN(r9), "max(nan, 1, 2, 3, 4, 5) should be NaN")

-- NaN in middle - gets skipped, max of non-NaN values before it wins
local r10 = math.max(1, nan, 2)
assert(r10 == 2, "max(1, nan, 2) should be 2 - NaN in middle skipped")

local r11 = math.max(3, nan, 1)
assert(r11 == 3, "max(3, nan, 1) should be 3 - NaN in middle skipped, 3 > 1")

-- NaN as last argument - gets skipped
local r12 = math.max(1, 2, nan)
assert(r12 == 2, "max(1, 2, nan) should be 2 - NaN at end skipped")

local r13 = math.max(5, 3, 1, nan)
assert(r13 == 5, "max(5, 3, 1, nan) should be 5 - NaN at end skipped")

-- Multiple NaN values interspersed
local r14 = math.max(1, nan, 3, nan, 5)
assert(r14 == 5, "max(1, nan, 3, nan, 5) should be 5 - multiple NaN skipped")

local r15 = math.max(5, nan, 3, nan, 1)
assert(r15 == 5, "max(5, nan, 3, nan, 1) should be 5")

-- ============================================================
-- SECTION 5: NaN with infinity
-- ============================================================
-- inf as first arg, nan as second - inf wins (nan skipped)
local r16 = math.max(inf, nan)
assert(r16 == inf, "max(inf, nan) should be inf - inf wins over NaN")

-- nan as first arg, inf as second - nan wins (comparison fails)
local r17 = math.max(nan, inf)
assert(isNaN(r17), "max(nan, inf) should be NaN - NaN as first arg wins")

-- Negative infinity cases
local r18 = math.max(-inf, nan)
assert(r18 == -inf, "max(-inf, nan) should be -inf - -inf wins over NaN")

local r19 = math.max(nan, -inf)
assert(isNaN(r19), "max(nan, -inf) should be NaN - NaN as first arg wins")

-- Multi-arg with inf and nan
local r20 = math.max(1, inf, nan)
assert(r20 == inf, "max(1, inf, nan) should be inf")

local r21 = math.max(nan, inf, 1)
assert(isNaN(r21), "max(nan, inf, 1) should be NaN - NaN first")

-- ============================================================
-- SECTION 6: NaN with negative zero
-- ============================================================
local negzero = -0.0
local r22 = math.max(nan, negzero)
assert(isNaN(r22), "max(nan, -0) should be NaN - NaN as first arg")

local r23 = math.max(negzero, nan)
assert(r23 == 0, "max(-0, nan) should be 0 (or -0) - non-NaN wins")

return "pass"
