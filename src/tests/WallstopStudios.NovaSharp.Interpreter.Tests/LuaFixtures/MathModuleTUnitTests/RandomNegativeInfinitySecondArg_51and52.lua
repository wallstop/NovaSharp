-- Tests that math.random(m, -inf) THROWS in Lua 5.1/5.2
-- Verified empirically: Both Lua 5.1 and 5.2 throw "interval is empty"
-- because 1 <= -inf is FALSE

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
local neginf = -1 / 0
local result = math.random(1, neginf)
-- Should throw "interval is empty" in Lua 5.1/5.2
print("ERROR: Should have thrown")