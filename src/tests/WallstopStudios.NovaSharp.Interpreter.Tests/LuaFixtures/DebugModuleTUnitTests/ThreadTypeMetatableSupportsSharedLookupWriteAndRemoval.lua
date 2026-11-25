-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2153
-- @test: DebugModuleTUnitTests.ThreadTypeMetatableSupportsSharedLookupWriteAndRemoval

local first = coroutine.create(function() end)
local second = coroutine.create(function()
    local running = coroutine.running()
    return debug.getmetatable(running)
end)
assert(debug.getmetatable(first) == nil)
local backing = {'before'}
local metadata = {
    __metatable = 'protected',
    __len = function() return 1 end,
    __eq = function() error('thread equality must use identity') end,
    __index = backing,
    __newindex = backing,
}
local resultCount = select('#', debug.setmetatable(first, metadata))
assert(resultCount == 1)
local returned = debug.setmetatable(first, metadata)
local expected = _VERSION == 'Lua 5.1' and true or first
assert(returned == expected)
assert(debug.getmetatable(second) == metadata)
assert(not pcall(debug.setmetatable, first, false))
assert(debug.getmetatable(first) == metadata)
assert(getmetatable(second) == 'protected')
local main = coroutine.running()
if main then assert(debug.getmetatable(main) == metadata) end
local ok, resumedMetatable = coroutine.resume(second)
assert(ok and resumedMetatable == metadata)
assert(first == first and first ~= second)
local file = io.stdout
assert(first ~= file and file ~= first)
assert(second[1] == 'before')
second.marker = 'written'
assert(first.marker == 'written' and backing.marker == 'written')

local inserted = pcall(table.insert, second, 1, 'after')
local supportsInsert = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
assert(inserted == supportsInsert)
assert(backing[1] == (supportsInsert and 'after' or 'before'))
assert(backing[2] == (supportsInsert and 'before' or nil))

local replacement = {
    __index = function(t, key)
        assert(t == first)
        return backing[key]
    end,
    __newindex = function(t, key, value)
        assert(t == second)
        backing[key] = value
    end,
}
assert(debug.setmetatable(second, replacement) == (_VERSION == 'Lua 5.1' and true or second))
assert(debug.getmetatable(first) == replacement)
second.marker = 'rewritten'
assert(first.marker == 'rewritten' and backing.marker == 'rewritten')
assert(debug.setmetatable(first, nil) == expected)
assert(debug.getmetatable(first) == nil and debug.getmetatable(second) == nil)
assert(not pcall(function() return first.marker end))
assert(not pcall(function() second.marker = nil end))
print('thread metatable scope, dispatch, and removal verified')
