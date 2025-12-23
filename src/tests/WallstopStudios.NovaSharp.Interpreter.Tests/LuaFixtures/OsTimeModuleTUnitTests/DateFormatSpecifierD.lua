-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %D should return MM/DD/YY format
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%D", 0)
-- Expected: 01/01/70
