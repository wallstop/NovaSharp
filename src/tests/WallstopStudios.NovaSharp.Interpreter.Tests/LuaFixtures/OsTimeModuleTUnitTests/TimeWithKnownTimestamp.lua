-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Test os.time with a known date (Y2K: Jan 1, 2000 00:00:00 UTC)
local t = os.time({year=2000, month=1, day=1, hour=0, min=0, sec=0})
-- Expected timestamp: 946684800
return t
