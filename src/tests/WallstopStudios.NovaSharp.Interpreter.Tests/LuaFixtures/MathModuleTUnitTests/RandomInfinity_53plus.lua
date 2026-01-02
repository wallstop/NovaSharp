-- Tests that math.random(inf) rejects infinity in Lua 5.3+
-- Verified empirically: throws "number has no integer representation"

-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
local inf = 1 / 0
math.random(inf)
print("ERROR: Should have thrown")