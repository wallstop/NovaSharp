-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %z should return timezone offset
-- With UTC prefix (!), should return +0000
return os.date("!%z", 0)
-- Expected: +0000
