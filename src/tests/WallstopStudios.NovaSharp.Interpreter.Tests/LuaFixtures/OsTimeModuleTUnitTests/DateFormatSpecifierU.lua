-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %U should return week number with Sunday as first day (00-53)
-- Epoch timestamp 0 is Thursday, January 1, 1970 - week 00
return os.date("!%U", 0)
-- Expected: 00
