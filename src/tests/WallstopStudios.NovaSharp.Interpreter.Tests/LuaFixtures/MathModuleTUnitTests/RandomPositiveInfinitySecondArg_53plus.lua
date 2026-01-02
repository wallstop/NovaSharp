-- Tests that math.random(m, inf) throws in Lua 5.3+
-- Verified empirically: throws "number has no integer representation"

-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
local inf = 1 / 0
math.random(1, inf)
print("ERROR: Should have thrown")