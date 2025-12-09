-- @lua-versions: 5.1, 5.2
-- Tests that math.random(n) accepts fractional values in Lua 5.1/5.2
-- These versions silently truncate via floor

-- This should succeed in 5.1/5.2 (fractional is truncated)
local result = math.random(2.9)
assert(result >= 1 and result <= 2, "Result should be in [1, 2] range")
print("PASS: math.random(2.9) accepted in Lua " .. _VERSION)
