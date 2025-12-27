-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %t should return a tab character
return os.date("!%t", 0) == "\t"
-- Expected: true
