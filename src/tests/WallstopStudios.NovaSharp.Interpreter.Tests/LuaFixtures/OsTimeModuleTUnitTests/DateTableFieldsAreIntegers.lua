-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- os.date("*t") table fields should be integers in Lua 5.3+
local dt = os.date("!*t", 0)
local results = {}
for _, field in ipairs({"year", "month", "day", "hour", "min", "sec", "wday", "yday"}) do
    local fieldType = math.type(dt[field])
    assert(fieldType == "integer", "Expected " .. field .. " to be integer, got " .. tostring(fieldType))
    results[#results + 1] = field .. "=" .. fieldType
end
return table.concat(results, ",")
