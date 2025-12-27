-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %V should return ISO week number (01-53)
-- Epoch timestamp 0 is Thursday, January 1, 1970 - ISO week 01
return os.date("!%V", 0)
-- Expected: 01
