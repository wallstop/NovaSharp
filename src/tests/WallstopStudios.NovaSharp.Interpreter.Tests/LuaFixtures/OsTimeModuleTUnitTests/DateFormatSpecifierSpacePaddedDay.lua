-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %e should return space-padded day of month
-- January 1 should be " 1" (space-padded to 2 chars)
return os.date("!%e", 0)
-- Expected:  1 (with leading space)
