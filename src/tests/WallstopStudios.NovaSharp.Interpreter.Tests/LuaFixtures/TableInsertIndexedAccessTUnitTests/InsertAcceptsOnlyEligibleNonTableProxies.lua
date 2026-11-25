-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableInsertIndexedAccessTUnitTests.cs:23
-- @test: TableInsertIndexedAccessTUnitTests.InsertAcceptsOnlyEligibleNonTableProxies
local indexed = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local high = 4294967298
local backing = {[high] = 'before'}
local nextTarget = setmetatable({}, {__index = backing, __newindex = backing})
local mt = {}
local lengthCalls = 0
mt.__len = function(first, second)
    assert(first == false and second == false)
    lengthCalls = lengthCalls + 1
    return high
end
mt.__index = nextTarget
mt.__newindex = nextTarget
debug.setmetatable(false, mt)
local ok, count = pcall(function()
    return select('#', table.insert(false, high, 'after'))
end)
assert(ok == indexed, 'non-table proxy eligibility')
assert(backing[high] == (indexed and 'after' or 'before'), 'proxy destination')
assert(backing[high + 1] == (indexed and 'before' or nil), 'wide proxy shift')

mt.__index = nil
setmetatable(mt, {__index = {__index = backing}})
ok = pcall(table.insert, false, 'would only write')
assert(not ok, 'inherited hook is ineligible even for append')
assert(lengthCalls == (indexed and 1 or 0), 'eligibility checked before length')
mt.__index = false
ok, count = pcall(function()
    return select('#', table.insert(false, 'only writes'))
end)
assert(ok == indexed, 'raw false read hook is present')
if indexed then
    assert(count == 0 and backing[high + 1] == 'only writes', 'append does not read')
end
debug.setmetatable(false, nil)

local oldStringMetatable = debug.getmetatable('')
local newStringMetatable = {__index = backing, __newindex = backing}
debug.setmetatable('', newStringMetatable)
local withoutLength = pcall(table.insert, 'abc', 'missing __len')
newStringMetatable.__len = function() error('string length hook must not run') end
local withLength = pcall(table.insert, 'abc', 'built-in length')
debug.setmetatable('', oldStringMetatable)
assert(not withoutLength and withLength == indexed, 'string proxy eligibility')
assert(backing[4] == (indexed and 'built-in length' or nil), 'built-in string length')

local function protectedReceiver(key)
    assert(key == 1)
    error('protected read')
end
debug.setmetatable(protectedReceiver, {
    __len = function() return 1 end,
    __index = pcall,
    __newindex = backing
})
ok = pcall(table.insert, protectedReceiver, 1, 'protected')
debug.setmetatable(protectedReceiver, nil)
assert(ok == indexed, 'native read hook must run protected calls')
if indexed then
    assert(backing[1] == 'protected' and backing[2] == false, 'protected hook first result')
end
