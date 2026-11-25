-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableInsertIndexedAccessTUnitTests.cs:13
-- @test: TableInsertIndexedAccessTUnitTests.InsertUsesIndexedAccessInLua53AndLater
local indexed = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local events = {}
local function note(event)
    events[#events + 1] = event
end

local values = {'keep'}
local mt = {}
mt.__len = function(first, second)
    assert(first == values and second == values, 'length receiver')
    note('len')
    mt.__index = function(receiver, key)
        assert(receiver == values, 'read receiver')
        note('get:' .. key)
        if key == 3 then
            return 'virtual', 'ignored'
        end
    end
    mt.__newindex = function(receiver, key, ...)
        assert(select('#', ...) == 1, 'write value must be present even for an empty read')
        local value = ...
        assert(receiver == values, 'write receiver')
        note('set:' .. key .. ':' .. tostring(value))
        rawset(receiver, key, value)
        return 'ignored'
    end
    return 3
end
setmetatable(values, mt)

local count = select('#', table.insert(values, 1, 'new'))
assert(count == 0)
assert(rawget(values, 1) == 'new' and rawget(values, 2) == 'keep')
assert(rawget(values, 3) == nil)
assert(rawget(values, 4) == (indexed and 'virtual' or nil), 'virtual source value')
local expected = _VERSION == 'Lua 5.1' and '' or 'len'
if indexed then
    expected = 'len,get:3,set:4:virtual,get:2,set:3:nil,set:2:keep'
end
assert(table.concat(events, ',') == expected, 'positional hook order')

events = {}
if indexed then
    rawset(values, 4, nil)
end
assert(select('#', table.insert(values, nil)) == 0)
assert(rawget(values, 4) == nil)
expected = _VERSION == 'Lua 5.1' and '' or 'len'
if indexed then
    expected = 'len,set:4:nil'
end
assert(table.concat(events, ',') == expected, 'nil append hook order')
