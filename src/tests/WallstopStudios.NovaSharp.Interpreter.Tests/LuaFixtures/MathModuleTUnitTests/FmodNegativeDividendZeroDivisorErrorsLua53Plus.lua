-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @compat-notes: Lua 5.3+ throws "bad argument #2 to 'fmod' (zero)" for fmod(-x, 0)

-- This should throw an error in Lua 5.3+ even with negative dividend
return math.fmod(-5, 0)
