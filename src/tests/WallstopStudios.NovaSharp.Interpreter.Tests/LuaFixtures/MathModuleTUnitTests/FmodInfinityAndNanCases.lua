-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: All versions: fmod with infinity/NaN follows IEEE 754 rules

local inf = 1/0
local nan = 0/0

-- Helper to check NaN (NaN is not equal to itself)
local function isNaN(x)
    return x ~= x
end

-- fmod(inf, x) returns NaN (infinity has no remainder)
assert(isNaN(math.fmod(inf, 2)), "fmod(inf, 2) should be NaN")
assert(isNaN(math.fmod(-inf, 2)), "fmod(-inf, 2) should be NaN")
assert(isNaN(math.fmod(inf, -2)), "fmod(inf, -2) should be NaN")
assert(isNaN(math.fmod(-inf, -2)), "fmod(-inf, -2) should be NaN")

-- fmod(x, inf) returns x (finite x divided by infinity has full remainder)
assert(math.fmod(2, inf) == 2, "fmod(2, inf) should be 2")
assert(math.fmod(-2, inf) == -2, "fmod(-2, inf) should be -2")
assert(math.fmod(2, -inf) == 2, "fmod(2, -inf) should be 2")
assert(math.fmod(-2, -inf) == -2, "fmod(-2, -inf) should be -2")

-- fmod with NaN always returns NaN
assert(isNaN(math.fmod(nan, 2)), "fmod(nan, 2) should be NaN")
assert(isNaN(math.fmod(2, nan)), "fmod(2, nan) should be NaN")
assert(isNaN(math.fmod(nan, nan)), "fmod(nan, nan) should be NaN")
assert(isNaN(math.fmod(nan, inf)), "fmod(nan, inf) should be NaN")
assert(isNaN(math.fmod(inf, nan)), "fmod(inf, nan) should be NaN")

-- fmod(0, inf) should be 0
assert(math.fmod(0, inf) == 0, "fmod(0, inf) should be 0")
assert(math.fmod(0, -inf) == 0, "fmod(0, -inf) should be 0")

return "pass"
