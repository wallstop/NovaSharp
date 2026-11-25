-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableInsertIndexedAccessTUnitTests.cs:33
-- @test: TableInsertIndexedAccessTUnitTests.InsertStopsAfterAnIndexingError
local indexed = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local values, events = {1}, {}
local mt = {__len = function() return 3 end}
mt.__index = function(_, key)
    events[#events + 1] = 'get:' .. key
    if key == 2 then
        error('read failed')
    end
    return 3
end
mt.__newindex = function(receiver, key, value)
    events[#events + 1] = 'set:' .. key
    rawset(receiver, key, value)
end
setmetatable(values, mt)
local ok, err = pcall(table.insert, values, 1, 'new')
assert(ok ~= indexed)
if indexed then
    assert(string.find(err, 'read failed', 1, true))
    assert(table.concat(events, ',') == 'get:3,set:4,get:2')
    assert(rawget(values, 4) == 3)
    assert(rawget(values, 1) == 1 and rawget(values, 2) == nil)
else
    assert(#events == 0 and rawget(values, 1) == 'new')
end

local loop = {}
local loopMt = {__len = function() return 1 end, __index = loop, __newindex = loop}
setmetatable(loop, loopMt)
if indexed then
    ok, err = pcall(table.insert, loop, 1, 1)
    assert(not ok and string.find(err, "'__index' chain too long", 1, true))
    loopMt.__index = nil
    ok, err = pcall(table.insert, loop, 2, 1)
    assert(not ok and string.find(err, "'__newindex' chain too long", 1, true))

    local function chain(target, hook, hops)
        for _ = 1, hops do
            target = setmetatable({}, {[hook] = target})
        end
        return target
    end
    for _, hops in ipairs({1999, 2000, 2001}) do
        local source = chain({99}, '__index', hops)
        getmetatable(source).__len = function() return 1 end
        ok = pcall(table.insert, source, 1, 'new')
        assert(ok == (hops <= 2000), 'read hook limit')
        assert(rawget(source, 2) == (ok and 99 or nil), 'read hook result')

        local backing = {'old'}
        local target = chain(backing, '__newindex', hops)
        ok = pcall(table.insert, target, 'new')
        assert(ok == (hops <= 2000), 'write hook limit')
        assert(backing[1] == (ok and 'new' or 'old'), 'write hook result')
    end
end

local destination = setmetatable({}, {
    __newindex = function() coroutine.yield('unexpected yield') end,
})
local thread = coroutine.create(function() table.insert(destination, 1) end)
ok, err = coroutine.resume(thread)
assert(ok ~= indexed and coroutine.status(thread) == 'dead')
if indexed then
    assert(string.find(err, '-call boundary', 1, true))
    assert(next(destination) == nil)
else
    assert(rawget(destination, 1) == 1)
end
