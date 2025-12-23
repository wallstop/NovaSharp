-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- os.time() should return an integer subtype in Lua 5.3+
local t = os.time({year=2000, month=1, day=1, hour=0, min=0, sec=0})
local result = math.type(t)
assert(result == "integer", "Expected os.time() to return integer, got " .. tostring(result))
return result
