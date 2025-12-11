-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that math.random(n) rejects infinity in Lua 5.3+
-- Infinity has no integer representation

local inf = 1/0
math.random(inf)
print("ERROR: Should have thrown")
