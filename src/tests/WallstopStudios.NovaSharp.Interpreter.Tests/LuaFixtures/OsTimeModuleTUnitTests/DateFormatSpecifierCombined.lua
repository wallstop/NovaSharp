-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- Test combined format string
return os.date("!%Y-%m-%d %H:%M:%S", 0)
-- Expected: 1970-01-01 00:00:00
