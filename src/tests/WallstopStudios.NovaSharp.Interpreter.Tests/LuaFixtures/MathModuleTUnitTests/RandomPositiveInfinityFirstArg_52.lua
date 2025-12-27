-- Tests that math.random(inf, n) THROWS in Lua 5.2
-- Verified empirically: Lua 5.2 uses float comparison (inf <= 10 is false),
-- so throws "interval is empty"

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
local inf = 1 / 0
local result = math.random(inf, 10)
-- Should throw "interval is empty" in Lua 5.2
print("ERROR: Should have thrown")