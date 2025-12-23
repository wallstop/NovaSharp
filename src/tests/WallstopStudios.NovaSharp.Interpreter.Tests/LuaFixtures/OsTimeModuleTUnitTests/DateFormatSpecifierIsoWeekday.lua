-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %u should return weekday number (1-7, Monday=1)
-- Epoch timestamp 0 is Thursday = 4
return os.date("!%u", 0)
-- Expected: 4
