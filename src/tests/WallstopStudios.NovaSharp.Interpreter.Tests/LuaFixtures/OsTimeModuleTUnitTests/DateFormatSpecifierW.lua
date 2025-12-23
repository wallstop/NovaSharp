-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %w should return weekday number (0-6, Sunday=0)
-- Epoch timestamp 0 is Thursday = 4
return os.date("!%w", 0)
-- Expected: 4
