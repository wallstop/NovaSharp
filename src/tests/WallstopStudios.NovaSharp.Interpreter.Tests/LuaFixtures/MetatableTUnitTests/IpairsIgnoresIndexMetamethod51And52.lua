-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- Test: ipairs uses raw access in Lua 5.1/5.2 (ignores __index)
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
-- Expected: "" (empty - raw access finds nothing)
