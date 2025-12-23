-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %j should return day of year (001-366)
-- Epoch timestamp 0 is January 1, so day 001
return os.date("!%j", 0)
-- Expected: 001
