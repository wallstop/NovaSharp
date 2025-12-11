-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that math.random(n) requires integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.7: math.random arguments must have integer representation

-- This should error with "number has no integer representation" in 5.3+
math.random(1.5)
print("ERROR: Should have thrown")
