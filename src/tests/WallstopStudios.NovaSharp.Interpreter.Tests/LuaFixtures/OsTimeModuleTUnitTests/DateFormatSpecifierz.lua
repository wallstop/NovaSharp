-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %z should return timezone offset
-- With UTC prefix (!), should return +0000
return os.date("!%z", 0)
-- Expected: +0000
