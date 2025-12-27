-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %n should return a newline character
return os.date("!%n", 0) == "\n"
-- Expected: true
