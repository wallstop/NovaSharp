-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Test: ipairs with mixed raw values and __index values in Lua 5.3+
-- Raw values should be found first, __index used as fallback
local underlying = {"a", "b", "c", "d", "e"}
local proxy = {nil, "B", nil}  -- Has explicit values at index 2
setmetatable(proxy, {
    __index = underlying
})

local result = ""
for i, v in ipairs(proxy) do
    result = result .. i .. ":" .. v .. " "
end
return result
-- Expected: "1:a 2:B 3:c 4:d 5:e " - ipairs gets nil at 1, falls back to __index
