-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Test: ipairs respects __index metamethod in Lua 5.3+
-- In Lua 5.3+, ipairs should iterate through values returned by __index
local underlying = {10, 20, 30}
local proxy = {}
setmetatable(proxy, {
    __index = function(t, k)
        return underlying[k]
    end
})

local result = ""
for i, v in ipairs(proxy) do
    result = result .. i .. ":" .. v .. " "
end
return result
-- Expected: "1:10 2:20 3:30 "
