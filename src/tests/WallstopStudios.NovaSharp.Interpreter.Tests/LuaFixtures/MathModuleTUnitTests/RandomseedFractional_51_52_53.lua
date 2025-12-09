-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @compat-notes: Tests that math.randomseed(x) accepts fractional values in Lua 5.1/5.2/5.3.
-- Only Lua 5.4+ requires integer representation for randomseed.

-- This should succeed in 5.1/5.2/5.3
math.randomseed(1.5)
local r1 = math.random()
math.randomseed(1.5)
local r2 = math.random()
-- After seeding with same value, random should return same sequence
assert(r1 == r2, "Seeding with 1.5 should be deterministic")
print("PASS")
