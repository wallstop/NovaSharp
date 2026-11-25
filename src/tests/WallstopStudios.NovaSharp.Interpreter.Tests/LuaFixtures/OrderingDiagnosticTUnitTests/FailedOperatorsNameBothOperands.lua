-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/OrderingDiagnosticTUnitTests.cs:13
-- @test: OrderingDiagnosticTUnitTests.FailedOperatorsNameBothOperands

local modern = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local actor = setmetatable({}, { __name = 'Actor', __metatable = 'locked' })
local prop = setmetatable({}, { __name = 'Prop' })
local same = setmetatable({}, { __name = 'Actor' .. string.char(0) .. 'suffix' })
local namedNumber = setmetatable({}, { __name = 'number' })
local unnamed = setmetatable({}, { __name = 123 })
local inherited = setmetatable({}, setmetatable({}, {
    __index = function() error('metafield access must be raw') end
}))
local actorName = modern and 'Actor' or 'table'
local function lt(left, right) return left < right end
local function le(left, right) return left <= right end
local function check(compare, left, right, leftName, rightName)
    local ok, err = pcall(compare, left, right)
    local expected = leftName == rightName
        and ('attempt to compare two ' .. leftName .. ' values')
        or ('attempt to compare ' .. leftName .. ' with ' .. rightName)
    assert(not ok and err:match('attempt to compare.*') == expected, tostring(err))
end

check(lt, 1, actor, 'number', actorName)
check(le, actor, 1, actorName, 'number')
check(lt, actor, prop, actorName, modern and 'Prop' or 'table')
check(le, actor, same, actorName, actorName)
check(lt, namedNumber, 1, modern and 'number' or 'table', 'number')
check(le, unnamed, inherited, 'table', 'table')
check(lt, {}, actor, 'table', actorName)
check(le, 1, io.stdout, 'number', modern and 'FILE*' or 'userdata')

local booleanMetatable = debug.getmetatable(true)
debug.setmetatable(true, { __name = 'Flag' })
check(lt, true, actor, 'boolean', actorName)
debug.setmetatable(true, booleanMetatable)

local function pass() return true end
local function fail() return false end
local mt = { __name = 'Comparable', __lt = pass, __le = fail }
local left, right = setmetatable({}, mt), setmetatable({}, mt)
assert(left < right)
assert(not (left <= right))
