-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false

local operand = setmetatable({}, { __sub = function()
    markHookEntered()
    local sum = 0
    for i = 1, 1000 do
        sum = sum + i
    end
    return sum
end })
return function() return operand - operand end
