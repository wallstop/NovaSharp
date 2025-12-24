-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: All versions: floating point fmod preserves sign-of-dividend rule

-- Test floating point math.fmod behavior
-- Sign of result should still match dividend

-- Positive dividend
assert(math.fmod(5.5, 2.5) == 0.5, "fmod(5.5, 2.5) should be 0.5")
assert(math.fmod(5.5, -2.5) == 0.5, "fmod(5.5, -2.5) should be 0.5")

-- Negative dividend
assert(math.fmod(-5.5, 2.5) == -0.5, "fmod(-5.5, 2.5) should be -0.5")
assert(math.fmod(-5.5, -2.5) == -0.5, "fmod(-5.5, -2.5) should be -0.5")

-- Mixed integer/float
assert(math.fmod(7, 2.5) == 2, "fmod(7, 2.5) should be 2")
assert(math.fmod(7.5, 3) == 1.5, "fmod(7.5, 3) should be 1.5")

-- Small remainders
assert(math.fmod(10.5, 3.5) == 0, "fmod(10.5, 3.5) should be 0")
assert(math.fmod(10.75, 3.5) == 0.25, "fmod(10.75, 3.5) should be 0.25")

return "pass"
