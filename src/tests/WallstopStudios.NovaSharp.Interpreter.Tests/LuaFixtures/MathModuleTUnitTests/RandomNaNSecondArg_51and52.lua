-- Tests that math.random(m, nan) THROWS in Lua 5.1/5.2
-- Verified empirically: Both Lua 5.1 and 5.2 throw "interval is empty"

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
local nan = 0 / 0
local result = math.random(1, nan)
-- Should throw "interval is empty" in Lua 5.1/5.2
print("ERROR: Should have thrown")