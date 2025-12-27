-- Tests that math.random(m, inf) does NOT throw in Lua 5.2
-- Verified empirically: Lua 5.2 uses float comparison (1.0 <= inf is true),
-- so interval check passes and returns inf due to overflow

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
local inf = 1 / 0
local result = math.random(1, inf)
-- Should NOT throw in Lua 5.2, returns inf due to overflow
print(type(result) == "number")