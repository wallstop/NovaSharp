-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %a should return abbreviated weekday name
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%a", 0)
-- Expected: Thu
