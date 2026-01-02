-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %b should return abbreviated month name
-- Epoch timestamp 0 is Thursday, January 1, 1970
return os.date("!%b", 0)
-- Expected: Jan
