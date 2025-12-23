-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %r should return 12-hour time with AM/PM
-- Epoch timestamp 0 is Thursday, January 1, 1970 00:00:00 UTC (12:00:00 AM)
return os.date("!%r", 0)
-- Expected: 12:00:00 AM
