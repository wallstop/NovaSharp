-- Tests that math.random(-inf, n) throws in Lua 5.3+
-- Verified empirically: throws "number has no integer representation"

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
local neginf = -1 / 0
math.random(neginf, 10)
print("ERROR: Should have thrown")