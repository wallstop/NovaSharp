-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %I should return 12-hour format hour (01-12)
-- Epoch timestamp 0 is 00:00:00 UTC, which is 12 in 12-hour format
return os.date("!%I", 0)
-- Expected: 12
