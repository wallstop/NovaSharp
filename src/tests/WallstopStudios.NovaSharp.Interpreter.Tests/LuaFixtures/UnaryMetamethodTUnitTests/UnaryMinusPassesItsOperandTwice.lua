-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local operand = {}
setmetatable(operand, {
    __unm = function(...)
        local first, second = ...
        assert(select("#", ...) == 2)
        assert(rawequal(first, operand) and rawequal(second, operand))
        return 9
    end,
})
assert(-operand == 9)

local number = 3
assert(-number == -3)

local function noResults() end
local nilMetatable = debug.getmetatable(nil)
debug.setmetatable(nil, { __unm = function(...)
    local first, second = ...
    assert(select("#", ...) == 2 and first == nil and second == nil)
    return 9
end })
assert(-noResults() == 9)
debug.setmetatable(nil, nilMetatable)
