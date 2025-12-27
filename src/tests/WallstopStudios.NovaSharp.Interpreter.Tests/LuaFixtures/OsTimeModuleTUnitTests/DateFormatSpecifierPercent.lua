-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- %% should return a literal percent sign
return os.date("!%%", 0)
-- Expected: %
