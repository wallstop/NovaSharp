-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local current = 0
local function update()
    current = 2
    return 1
end
assert(not (current < update()), 'read local after right-side call')
current = 0
assert(not (current <= update()), 'read local after right-side call <=')
current = 0
assert(not (((current)) < update()), 'read parenthesized local after right-side call')
current = 0
assert(not (((current)) <= update()), 'read parenthesized local after right-side call <=')

-- Computed left operands are snapshotted before the right operand runs.
current = 0
assert((current + 0) < update(), 'snapshot computed left operand before right-side call')
current = 0
assert((current + 0) <= update(), 'snapshot computed left operand before right-side call <=')

local upvalue = 0
local function updateUpvalue()
    upvalue = 2
    return 1
end
local function compareUpvalue()
    return upvalue < updateUpvalue()
end
assert(compareUpvalue(), 'snapshot upvalue before right-side call')
upvalue = 0
local function compareUpvalueOrEqual()
    return upvalue <= updateUpvalue()
end
assert(compareUpvalueOrEqual(), 'snapshot upvalue before right-side call <=')

comparisonGlobal = 0
local function updateGlobal()
    comparisonGlobal = 2
    return 1
end
assert(comparisonGlobal < updateGlobal(), 'snapshot global before right-side call')
comparisonGlobal = 0
assert(comparisonGlobal <= updateGlobal(), 'snapshot global before right-side call <=')

local indexed = {0}
local function updateIndexed()
    indexed[1] = 2
    return 1
end
assert(indexed[1] < updateIndexed(), 'snapshot indexed operand before right-side call')
indexed[1] = 0
assert(indexed[1] <= updateIndexed(), 'snapshot indexed operand before right-side call <=')

-- A metatable read on the right still sees the live local.
local probe = setmetatable({}, {
    __index = function()
        current = 4
        return 1
    end
})
current = 0
assert(not (current < probe.value), 'right __index updates the compared local')
current = 0
assert(not (current <= probe.value), 'right __index updates the compared local <=')

-- Comparison hooks receive the live local and the right operand in order.
local trace = ''
local mt = {
    __lt = function(left, right)
        trace = trace .. left.tag .. right.tag
        return left.tag < right.tag
    end,
    __le = function(left, right)
        trace = trace .. 'e' .. left.tag .. right.tag
        return left.tag <= right.tag
    end,
}
local hooked = setmetatable({tag = 'a'}, mt)
local function makeHooked()
    current = 2
    return setmetatable({tag = 'b'}, mt)
end
current = 0
assert(hooked < makeHooked(), 'hook result with live local')
assert(trace == 'ab', 'hook arguments after right-side call')
trace = ''
current = 0
assert(hooked <= makeHooked(), 'hook result with live local <=')
assert(trace == 'eab', 'hook arguments after right-side call <=')

-- Errors from the right operand surface before the comparison runs.
local function boom()
    error('boom', 0)
end
local ok, message = pcall(function()
    return current < boom()
end)
assert(not ok and message == 'boom', 'right operand error precedence')
ok, message = pcall(function()
    return hooked <= boom()
end)
assert(not ok and message == 'boom', 'right operand error precedence <=')

-- Local-to-local comparisons keep their direct path and results.
local left, right = 1, 2
assert(left < right and not (right < left))
assert(left <= left and left <= right and not (right <= left))
