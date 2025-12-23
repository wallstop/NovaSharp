-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- os.difftime second argument is optional in Lua 5.1/5.2
-- Returns first argument when second is omitted
return os.difftime(200)
-- Expected: 200
