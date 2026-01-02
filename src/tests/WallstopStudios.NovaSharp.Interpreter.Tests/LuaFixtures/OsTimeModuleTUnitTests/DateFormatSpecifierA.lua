-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %a should return abbreviated weekday name
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%a", 0)
-- Expected: Thu
