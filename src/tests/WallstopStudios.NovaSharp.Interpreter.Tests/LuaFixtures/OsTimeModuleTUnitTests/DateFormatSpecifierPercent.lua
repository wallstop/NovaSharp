-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- %% should return a literal percent sign
return os.date("!%%", 0)
-- Expected: %
