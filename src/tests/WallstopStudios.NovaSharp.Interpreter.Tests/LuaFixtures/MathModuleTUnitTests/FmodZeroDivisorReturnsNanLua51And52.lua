-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: Lua 5.1/5.2 return NaN for fmod(x, 0); Lua 5.3+ throws error

-- Test that math.fmod(x, 0) returns NaN in Lua 5.1/5.2
local result1 = math.fmod(5, 0)
local result2 = math.fmod(-5, 0)
local result3 = math.fmod(0, 0)
local result4 = math.fmod(1.5, 0)

-- NaN is the only value that is not equal to itself
local function isNaN(x)
    return x ~= x
end

-- All results should be NaN
assert(isNaN(result1), "fmod(5, 0) should be NaN")
assert(isNaN(result2), "fmod(-5, 0) should be NaN")
assert(isNaN(result3), "fmod(0, 0) should be NaN")
assert(isNaN(result4), "fmod(1.5, 0) should be NaN")

return "pass"
