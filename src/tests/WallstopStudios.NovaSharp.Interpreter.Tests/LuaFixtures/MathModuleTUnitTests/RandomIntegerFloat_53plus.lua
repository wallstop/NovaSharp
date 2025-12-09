-- @lua-versions: 5.3, 5.4
-- Tests that math.random(n) accepts float values that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.7: 5.0 has integer representation

-- This should succeed - 5.0 is a float but has integer representation
local result = math.random(5.0)
assert(result >= 1 and result <= 5, "Result should be in [1, 5] range")
print("PASS: math.random(5.0) accepted")
