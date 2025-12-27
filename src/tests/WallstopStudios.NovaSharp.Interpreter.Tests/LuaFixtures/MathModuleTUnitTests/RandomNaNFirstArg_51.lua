-- Tests that math.random(nan, n) does NOT throw in Lua 5.1
-- Verified empirically: Lua 5.1 converts nan to long (LONG_MIN),
-- comparison LONG_MIN <= 10 is TRUE, so no error; returns a number

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
local nan = 0 / 0
local result = math.random(nan, 10)
-- Should NOT throw in Lua 5.1, returns a number
print(type(result) == "number")