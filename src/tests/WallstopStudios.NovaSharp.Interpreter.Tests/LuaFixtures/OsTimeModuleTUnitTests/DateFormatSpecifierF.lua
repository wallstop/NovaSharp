-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %F should return YYYY-MM-DD (ISO 8601 date format)
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%F", 0)
-- Expected: 1970-01-01
