-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variables: bridge, zeroBridge, argsBridge, callBridge
local object = setmetatable({}, { __add = bridge })

local function capture(value)
    return value
end

local function forward(...)
    return ...
end

local function pack(...)
    return select('#', ...), ...
end

local directCount, direct = pack(bridge())
assert(directCount == 1, 'direct tail call result count: ' .. tostring(directCount))
assert(direct == 'recovered', 'direct tail call result: ' .. tostring(direct))

local metamethod = capture(object + object)
assert(metamethod == 'recovered', 'metamethod tail call result: ' .. tostring(metamethod))

local zeroCount = select('#', zeroBridge())
assert(zeroCount == 0, 'zero-result recovery count: ' .. tostring(zeroCount))

local argumentCount, argumentResult = pack(argsBridge())
assert(argumentCount == 1, 'argument recovery count: ' .. tostring(argumentCount))
assert(argumentResult == 'recovered', 'argument recovery value: ' .. tostring(argumentResult))

local callResult = capture(callBridge())
assert(callResult == 'recovered', 'callable table recovery value: ' .. tostring(callResult))

local left, middle, right = forward('left', bridge(), 'right')
assert(left == 'left', 'left caller argument: ' .. tostring(left))
assert(middle == 'recovered', 'middle caller argument: ' .. tostring(middle))
assert(right == 'right', 'right caller argument: ' .. tostring(right))

return direct .. '|' .. metamethod .. '|' .. tostring(zeroCount)
