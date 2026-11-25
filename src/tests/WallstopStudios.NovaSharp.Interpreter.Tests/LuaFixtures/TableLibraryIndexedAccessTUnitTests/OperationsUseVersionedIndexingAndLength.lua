-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryIndexedAccessTUnitTests.cs:13
-- @test: TableLibraryIndexedAccessTUnitTests.OperationsUseVersionedIndexingAndLength
local legacy = _VERSION == 'Lua 5.1' or _VERSION == 'Lua 5.2'
local unpack_values = table.unpack or unpack
local function list(values)
    local log = {}
    local function note(action)
        log[#log + 1] = action
    end
    local object = legacy and values or {}
    return setmetatable(object, {
        __len = function()
            note('len')
            return rawlen(values)
        end,
        __index = function(_, key)
            note('r' .. key)
            return rawget(values, key), 'discard'
        end,
        __newindex = function(_, key, value)
            note('w' .. key .. '=' .. tostring(value))
            rawset(values, key, value)
        end
    }), function() return table.concat(log, ',') end
end

local items, trace = list({'c', 'a', 'b'})
assert(table.concat(items, '-', 1, 2) == 'c-a')
assert(trace() == (legacy and '' or 'len,r1,r2'), 'concat explicit end')
items, trace = list({'c', 'a', 'b'})
assert(table.concat(items, '-') == 'c-a-b')
assert(trace() == (_VERSION == 'Lua 5.1' and '' or legacy and 'len' or 'len,r1,r2,r3'), 'concat default end')

items, trace = list({'a', 'b'})
local first, second, third = unpack_values(items, 1, 3)
assert(first == 'a' and second == 'b' and third == nil)
assert(trace() == (legacy and '' or 'r1,r2,r3'), 'unpack explicit end')
items, trace = list({'a', 'b'})
first, second = unpack_values(items)
assert(first == 'a' and second == 'b')
assert(trace() == (_VERSION == 'Lua 5.1' and '' or legacy and 'len' or 'len,r1,r2'), 'unpack default end')

local stored = {'a', 'b', 'c'}
items, trace = list(stored)
assert(table.remove(items, 1) == 'a' and stored[1] == 'b' and stored[2] == 'c' and rawget(stored, 3) == nil)
assert(trace() == (_VERSION == 'Lua 5.1' and '' or legacy and 'len' or 'len,r1,r2,w1=b,r3,w2=c,w3=nil'), 'remove shift')

stored = {'c', 'a', 'b'}
items, trace = list(stored)
assert(select('#', table.sort(items)) == 0)
assert(stored[1] == 'a' and stored[2] == 'b' and stored[3] == 'c')
assert(trace() == (_VERSION == 'Lua 5.1' and '' or legacy and 'len' or 'len,r1,r3,w1=b,w3=c,r2,r1,w2=b,w1=a'), 'sort in-place reads and writes')

if not legacy then
    stored = {'a', nil, 'c'}
    items, trace = list(stored)
    assert(rawequal(table.move(items, 1, 3, 2), items))
    assert(stored[1] == 'a' and stored[2] == 'a' and stored[3] == nil and stored[4] == 'c')
    assert(trace() == 'r3,w4=c,r2,w3=nil,r1,w2=a', 'move overlapping shift')
end

local short = {'b', 'd'}
if _VERSION == 'Lua 5.1' then
    assert(select('#', table.remove(short, 7)) == 0, 'out-of-range remove returns no value')
else
    local argument = (_VERSION == 'Lua 5.4' or _VERSION == 'Lua 5.5') and 2 or 1
    local ok, result = pcall(function() table.remove(short, 7) end)
    assert(not ok and result:find('bad argument #' .. argument, 1, true), 'out-of-range remove diagnostic')
end

if _VERSION == 'Lua 5.2' then
    local size = 2147483647
    local limit = setmetatable({[size - 1] = 'old', [size] = 'next'}, {
        __len = function() return size end
    })
    local ok, result = pcall(function() table.remove(limit, size - 1) end)
    assert(not ok and result:find('position out of bounds', 1, true), 'remove validates the wrapped C int bound')
    assert(rawget(limit, size - 1) == 'old' and rawget(limit, size) == 'next', 'failed validation leaves values intact')
end
