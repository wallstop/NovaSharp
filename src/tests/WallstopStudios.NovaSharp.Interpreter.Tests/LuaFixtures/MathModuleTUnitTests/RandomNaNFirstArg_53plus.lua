-- Tests that math.random(nan, n) throws in Lua 5.3+
-- Verified empirically: throws "number has no integer representation"

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
local nan = 0 / 0
math.random(nan, 10)
print("ERROR: Should have thrown")