-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %C should return century (00-99)
-- 1970 is in the 19th century (1900-1999)
return os.date("!%C", 0)
-- Expected: 19
