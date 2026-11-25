-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local meta = {
    __add = hostMany,
    __sub = hostNone,
    __concat = hostTail,
    __unm = hostNativeTail,
}
local operand = setmetatable({}, meta)
local function check(expected, ...)
    assert(select("#", ...) == 1 and (...) == expected)
end
check(7, operand + operand)
check(nil, operand - operand)
check(7, operand .. operand)
check(7, -operand)
assert(select("#", hostMany()) == 3 and select("#", hostNone()) == 0)
