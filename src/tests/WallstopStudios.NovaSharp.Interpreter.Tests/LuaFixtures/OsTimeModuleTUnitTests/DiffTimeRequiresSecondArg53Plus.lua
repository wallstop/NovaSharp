-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- os.difftime requires second argument in Lua 5.3+
-- This should throw an error
return os.difftime(200)
