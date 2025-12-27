-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- Test: ipairs respects __index table (not function) in Lua 5.3+
local underlying = {100, 200, 300, 400}
local proxy = {}
setmetatable(proxy, {
    __index = underlying
})

local result = ""
for i, v in ipairs(proxy) do
    result = result .. i .. ":" .. v .. " "
end
return result
-- Expected: "1:100 2:200 3:300 4:400 "
