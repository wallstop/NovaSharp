-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: All versions: sign of result matches sign of dividend (C fmod behavior)

-- Test that sign of math.fmod result matches the dividend, not the divisor
-- This is the C fmod() behavior (truncate toward zero), not IEEE remainder

-- Positive dividend cases: result should be positive
assert(math.fmod(5, 3) == 2, "fmod(5, 3) should be 2")
assert(math.fmod(5, -3) == 2, "fmod(5, -3) should be 2")

-- Negative dividend cases: result should be negative
assert(math.fmod(-5, 3) == -2, "fmod(-5, 3) should be -2")
assert(math.fmod(-5, -3) == -2, "fmod(-5, -3) should be -2")

-- Additional integer cases
assert(math.fmod(10, 3) == 1, "fmod(10, 3) should be 1")
assert(math.fmod(10, -3) == 1, "fmod(10, -3) should be 1")
assert(math.fmod(-10, 3) == -1, "fmod(-10, 3) should be -1")
assert(math.fmod(-10, -3) == -1, "fmod(-10, -3) should be -1")

-- Edge case: zero dividend
assert(math.fmod(0, 5) == 0, "fmod(0, 5) should be 0")
assert(math.fmod(0, -5) == 0, "fmod(0, -5) should be 0")

return "pass"
