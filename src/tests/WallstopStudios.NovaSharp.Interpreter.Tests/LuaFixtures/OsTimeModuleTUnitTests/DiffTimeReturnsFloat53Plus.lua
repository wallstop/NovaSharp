-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- os.difftime should return a float in Lua 5.3+
local diff = os.difftime(200, 150)
-- Result should be 50.0 (float), not 50 (integer)
return math.type(diff)
-- Expected: float
