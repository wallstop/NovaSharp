-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %w should return weekday number (0-6, Sunday=0)
-- Epoch timestamp 0 is Thursday = 4
return os.date("!%w", 0)
-- Expected: 4
