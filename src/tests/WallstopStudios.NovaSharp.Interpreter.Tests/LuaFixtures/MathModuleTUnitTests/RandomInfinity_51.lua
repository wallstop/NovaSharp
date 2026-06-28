-- Tests that math.random(inf) THROWS in Lua 5.1
-- Verified empirically: Lua 5.1 converts inf to long (LONG_MIN),
-- then checks u < 1 which fails, throwing "interval is empty"

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
local inf = 1 / 0
local result = math.random(inf)
-- Should throw "interval is empty" in Lua 5.1
print("ERROR: Should have thrown")