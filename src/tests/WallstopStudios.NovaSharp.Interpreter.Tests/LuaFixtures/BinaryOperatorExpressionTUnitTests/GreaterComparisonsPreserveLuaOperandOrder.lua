-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local log = ''
local function operand(label, value)
    log = log .. label
    return value, 'unused second result'
end
local function fail(label)
    log = log .. label
    error(label, 0)
end

local function check(compare, label)
    log = ''
    assert(compare(function() return operand('L', 2) end, function() return operand('R', 1) end), label)
    assert(log == 'LR', label .. ' calls ran out of order: ' .. log)

    log = ''
    local ok, message = pcall(compare, function() fail('left') end, function() fail('right') end)
    assert(not ok and message == 'left' and log == 'left', label .. ' first error: ' .. log)

    log = ''
    ok, message = pcall(compare, function() return operand('L', 2) end, function() fail('right') end)
    assert(not ok and message == 'right' and log == 'Lright', label .. ' second error: ' .. log)

    local nan = 0 / 0
    assert(not compare(function() return nan end, function() return 1 end), label .. ' left NaN')
    assert(not compare(function() return 1 end, function() return nan end), label .. ' right NaN')
end

local function greater(left, right) return left() > right() end
local function greaterEqual(left, right) return left() >= right() end
check(greater, '>')
check(greaterEqual, '>=')

local function comparator(event)
    return function(left, right)
        log = log .. event .. left.label .. right.label
        return 'truthy comparator result'
    end
end
local lt = comparator('lt')
local explicit = {__lt = lt, __le = comparator('le')}
local ltOnly = {__lt = lt}

local function compareTables(compare, metatable)
    log = ''
    local left = setmetatable({label = 'a'}, metatable)
    local right = setmetatable({label = 'b'}, metatable)
    return pcall(compare, function() return operand('L', left) end, function() return operand('R', right) end)
end

local ok, result = compareTables(greater, explicit)
assert(ok and result == true and log == 'LRltba', 'reversed __lt arguments: ' .. log)
ok, result = compareTables(greaterEqual, explicit)
assert(ok and result == true and log == 'LRleba', 'reversed __le arguments: ' .. log)
ok, result = compareTables(greaterEqual, ltOnly)
if _VERSION == 'Lua 5.5' then
    assert(not ok and log == 'LR', 'Lua 5.5 has no __lt fallback: ' .. log)
else
    assert(ok and result == false and log == 'LRltab', '__lt fallback arguments: ' .. log)
end

-- A direct local stays in its register until the comparison uses it.
local current = 0
local function update()
    current = 2
    return 1
end
assert(current > update(), 'read local after right-side call')
current = 0
assert(((current)) >= update(), 'read parenthesized local after right-side call')
current = 0
assert(not ((current + 0) > update()), 'snapshot computed left operand before right-side call')
current = 0
assert(not (function() return current >= update() end)(), 'snapshot upvalue before right-side call')

comparisonGlobal = 0
local function updateGlobal()
    comparisonGlobal = 2
    return 1
end
assert(not (comparisonGlobal >= updateGlobal()), 'snapshot global before right-side call')
