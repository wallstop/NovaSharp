-- Tests that math.random(-inf, n) does NOT throw in Lua 5.1/5.2
-- Verified empirically: Both Lua 5.1 and 5.2 succeed (no error)

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
local neginf = -1 / 0
local result = math.random(neginf, 10)
-- Should not throw, should return a number
print(type(result) == "number")