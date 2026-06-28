-- Tests that math.random(nan, n) THROWS in Lua 5.2
-- Verified empirically: Lua 5.2 uses float comparison (nan <= 10 is false per IEEE 754),
-- so throws "interval is empty"

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
local nan = 0 / 0
local result = math.random(nan, 10)
-- Should throw "interval is empty" in Lua 5.2
print("ERROR: Should have thrown")