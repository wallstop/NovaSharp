-- Tests that math.random(-inf) THROWS in Lua 5.1/5.2
-- Verified empirically: Both Lua 5.1 and 5.2 throw "interval is empty"

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
local neginf = -1 / 0
local result = math.random(neginf)
-- Should throw "interval is empty" in Lua 5.1/5.2
print("ERROR: Should have thrown")