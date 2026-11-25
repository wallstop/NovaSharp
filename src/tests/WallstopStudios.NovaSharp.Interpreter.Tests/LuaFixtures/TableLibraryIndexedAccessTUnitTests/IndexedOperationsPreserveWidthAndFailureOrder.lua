-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryIndexedAccessTUnitTests.cs:33
-- @test: TableLibraryIndexedAccessTUnitTests.IndexedOperationsPreserveWidthAndFailureOrder
local function fails_with(address, call)
    local ok, result = pcall(call)
    assert(not ok and result:find(address, 1, true), address)
end

local high = math.maxinteger
local values = setmetatable({[high] = 'last'}, {__len = function() return high end})
assert(table.concat(values, '-', high, high) == 'last')
assert(table.unpack(values, high, high) == 'last')
assert(table.remove(values, high) == 'last' and rawget(values, high) == nil)
assert(table.move({'first'}, 1, 1, high, values) == values and rawget(values, high) == 'first')
fails_with('too many elements to move', function() table.move(values, math.mininteger, high, 1) end)
fails_with('destination wrap around', function() table.move(values, 1, 2, high) end)
fails_with('too many results to unpack', function() return table.unpack(values, math.mininteger, high) end)

local trace = {}
local function note(event) trace[#trace + 1] = event end
local items = setmetatable({}, {
    __len = function()
        note('len')
        return 3
    end,
    __index = function(_, key)
        note('r' .. key)
        if key == 3 then error('read error') end
        return key
    end,
    __newindex = function(_, key, value)
        note('w' .. key .. '=' .. tostring(value))
    end
})
fails_with('read error', function() table.remove(items, 1) end)
assert(table.concat(trace, ',') == 'len,r1,r2,w1=2,r3', 'remove stops at failed read')
trace = {}
fails_with('string expected', function() table.concat(items, false, 1, 0) end)
assert(table.concat(trace, ',') == 'len', 'concat length before argument validation')
trace = {}
fails_with('read error', function() table.sort(items, function() error('wrong comparator') end) end)
assert(table.concat(trace, ',') == 'len,r1,r3', 'sort stops before comparator on failed read')

trace = {}
local mt = {__eq = function()
    note('eq')
    return true
end}
local both = {'a', 'b', 'c'}
local source = setmetatable({}, {
    __eq = mt.__eq,
    __index = function(_, key)
        note('r' .. key)
        return both[key]
    end
})
local target = setmetatable({}, {
    __newindex = function(_, key, value)
        note('w' .. key)
        both[key] = value
    end
})
assert(rawequal(table.move(source, 1, 2, 2, target), target))
assert(both[1] == 'a' and both[2] == 'a' and both[3] == 'b')
assert(table.concat(trace, ',') == 'eq,r2,w3,r1,w2', 'move uses equality before choosing direction')

trace = {}
local backing = {4, 1, 3, 2}
items = setmetatable({}, {
    __len = function() return 4 end,
    __index = function(_, key)
        note('r' .. key)
        return backing[key]
    end,
    __newindex = function(_, key, value)
        note('w' .. key .. '=' .. value)
        backing[key] = value
    end
})
table.sort(items, function(a, b)
    note('c' .. a .. '<' .. b)
    return a < b
end)
assert(table.concat(backing, ',') == '1,2,3,4')
assert(table.concat(trace, ',') ==
    'r1,r4,c2<4,w1=2,w4=4,r2,r1,c1<2,w2=2,w1=1,r2,r3,w2=3,w3=2,r2,c3<2,r2,c2<3,r1,c2<1,w3=3,w2=2,r3,r4,c4<3',
    'sort partition read, compare, and write order')

trace = {}
backing = {3, 1, 2}
items = setmetatable({}, {
    __len = function() return 3 end,
    __index = backing,
    __newindex = function(_, key, value)
        note('w' .. key .. '=' .. value)
        if key == 3 then error('write error') end
        backing[key] = value
    end
})
fails_with('write error', function() table.sort(items) end)
assert(table.concat(trace, ',') == 'w1=2,w3=3' and backing[1] == 2 and backing[2] == 1, 'sort commits only completed writes')

assert(select('#', table.unpack(io.stdout, 1, 1)) == 1)
assert(table.unpack(io.stdout, 1, 1) == nil)
local file_proxy = setmetatable({}, {__index = io.stdout})
assert(table.unpack(file_proxy, 1, 1) == nil, 'index chain ending in userdata')

local copy = {'old'}
assert(rawequal(table.move(io.stdout, 1, 1, 1, copy), copy) and rawget(copy, 1) == nil, 'file userdata can be a move source')
assert(not pcall(table.move, copy, 1, 0, 1, io.stdout), 'file userdata lacks a write hook')
assert(not pcall(table.concat, io.stdout, '', 1, 0), 'file userdata lacks a length hook')
