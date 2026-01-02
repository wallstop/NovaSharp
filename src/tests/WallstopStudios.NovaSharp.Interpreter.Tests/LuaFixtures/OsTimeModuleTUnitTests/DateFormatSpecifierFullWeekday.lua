-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %A should return full weekday name
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%A", 0)
-- Expected: Thursday
