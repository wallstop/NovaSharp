-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that math.random(n) rejects NaN in Lua 5.3+
-- NaN has no integer representation

local nan = 0/0
math.random(nan)
print("ERROR: Should have thrown")
