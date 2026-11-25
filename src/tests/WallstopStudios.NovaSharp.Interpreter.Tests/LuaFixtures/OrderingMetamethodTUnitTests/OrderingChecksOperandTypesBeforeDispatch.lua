-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local is51 = _VERSION == 'Lua 5.1'
local trace = ''
local function hook(a, b)
    trace = trace .. 'H'
    return type(a) == 'number' or type(b) ~= 'number'
end
local metatable = { __lt = hook, __le = hook }
debug.setmetatable(true, metatable)
debug.setmetatable(0, metatable)
debug.setmetatable('', metatable)

local function step(label, value)
    trace = trace .. label
    return value
end
local comparisons = {
    {'<',  function(a, b) return step('L', a) <  step('R', b) end, false},
    {'<=', function(a, b) return step('L', a) <= step('R', b) end, false},
    {'>',  function(a, b) return step('L', a) >  step('R', b) end, true},
    {'>=', function(a, b) return step('L', a) >= step('R', b) end, true},
}
local firstThread = coroutine.create(function() end)
local secondThread = coroutine.create(function() end)
debug.setmetatable(firstThread, metatable)

for _, comparison in ipairs(comparisons) do
    local name, compare, reversed = comparison[1], comparison[2], comparison[3]
    for _, primitive in ipairs({true, firstThread}) do
        trace = ''
        local ok, result = pcall(compare, primitive, 1)
        if is51 then
            local primitiveType = type(primitive)
            local expected = reversed
                and ('attempt to compare number with ' .. primitiveType)
                or ('attempt to compare ' .. primitiveType .. ' with number')
            assert(not ok and string.find(result, expected, 1, true), name .. ': mixed comparison error')
            assert(trace == 'LR', name .. ': operand order and no mixed hook')
        else
            assert(ok and result == reversed, name .. ': mixed hook operand order')
            assert(trace == 'LRH', name .. ': operand order before mixed hook')
        end
    end

    trace = ''
    assert(compare(true, false) and compare(firstThread, secondThread), name .. ': same type hook')
    assert(trace == 'LRHLRH', name .. ': same type hook side effects')

    trace = ''
    local lower, higher = 1, 2
    if reversed then lower, higher = higher, lower end
    assert(compare(lower, higher), name .. ': numeric fast path')
    lower, higher = 'a', 'b'
    if reversed then lower, higher = higher, lower end
    assert(compare(lower, higher), name .. ': string fast path')
    assert(trace == 'LRLR', name .. ': primitive pairs bypass hooks')
end

-- Legacy and indexed table.sort both use Lua's default ordering. An explicit
-- comparator can always define an order for mixed types.
trace = ''
local mixed = {true, 1}
local ok, message = pcall(table.sort, mixed)
if is51 then
    assert(not ok and string.find(message, 'attempt to compare', 1, true), 'mixed sort error')
    assert(trace == '', 'mixed default sort bypasses the hook')
else
    assert(ok and mixed[1] == 1 and mixed[2] == true and trace ~= '', 'mixed default sort hook')
end
table.sort(mixed, function(a, b) return type(a) == 'boolean' and type(b) == 'number' end)
assert(mixed[1] == true and mixed[2] == 1, 'explicit mixed sort comparator')
