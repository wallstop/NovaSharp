-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Test os.date("*t") returns correct table fields
-- Epoch timestamp 0 is Thursday, January 1, 1970 00:00:00 UTC
local dt = os.date("!*t", 0)
local expected = {
    year = 1970,
    month = 1,
    day = 1,
    hour = 0,
    min = 0,
    sec = 0,
    wday = 5,  -- Thursday is weekday 5 (Sunday=1)
    yday = 1,
}
local results = {}
for k, v in pairs(expected) do
    if dt[k] ~= v then
        results[#results + 1] = k .. ": expected " .. tostring(v) .. ", got " .. tostring(dt[k])
    end
end
if dt.isdst ~= false then
    results[#results + 1] = "isdst: expected false, got " .. tostring(dt.isdst)
end
return #results == 0 and "PASS" or table.concat(results, "; ")
