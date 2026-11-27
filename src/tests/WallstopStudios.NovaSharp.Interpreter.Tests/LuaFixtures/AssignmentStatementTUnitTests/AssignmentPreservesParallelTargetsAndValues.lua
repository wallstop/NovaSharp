-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local log = ''
local function mark(value)
    log = log .. value
    return value
end

local left, right = {}, {}
left[mark('i')], right[mark('j')] = mark('x'), mark('y')
assert(log == 'ijxy', 'targets must be evaluated before values: ' .. log)

local setterLog = ''
local proxy = setmetatable({}, {
    __newindex = function(table, key, value)
        setterLog = setterLog .. key .. '=' .. tostring(value) .. ';'
        rawset(table, key, value)
        return 'discarded'
    end,
})
local kept = 'before'
proxy.x, kept = true, false
assert(proxy.x == true, 'newindex must store assigned value')
assert(kept == false, 'newindex return must not replace pending value')
assert(setterLog == 'x=true;', 'newindex arguments: ' .. setterLog)

local storeLog = ''
local function ordered(name)
    return setmetatable({}, {
        __newindex = function(table, key, value)
            storeLog = storeLog .. name .. key .. ';'
            rawset(table, key, value)
        end,
    })
end
local first, second, third = ordered('A'), ordered('B'), ordered('C')
first.x, second.y, third.z = 1, 2, 3
assert(storeLog == 'Cz;By;Ax;', 'stores must run right to left: ' .. storeLog)

local old, new = {}, {}
local target = old
target.x, target = 1, new
assert(old.x == 1 and new.x == nil, 'local target must be snapshotted on conflict')

local keyed = {}
local key = 'a'
keyed[key], key = 3, 'b'
assert(keyed.a == 3 and keyed.b == nil, 'local key must be snapshotted on conflict')

local duplicate
duplicate, duplicate = 1, 2
assert(duplicate == 1, 'later stores must not win')

local sharedProxy = setmetatable({}, {
    __newindex = function(table, key, value)
        rawset(table, key, value)
        return 99
    end,
})
local sharedLocal
local function twoValues()
    return 10, 20
end
sharedLocal, sharedProxy.x = twoValues()
assert(sharedLocal == 10 and sharedProxy.x == 20, 'setter must not replace the shared tuple')

local conflictOld, conflictNew = {}, {}
local conflictTarget = conflictOld
local conflictKey = 'a'
local function changeConflictTarget()
    conflictTarget = conflictNew
    return 'v'
end
conflictTarget[conflictKey], conflictKey = changeConflictTarget(), 'b'
assert(conflictOld.a == nil, 'conflicting key copy must not snapshot the table')
assert(conflictNew.a == 'v', 'unconflicted table must be read at store time')
assert(conflictKey == 'b', 'conflicting key must still be assigned')

local laterOld, laterNew = {}, {}
local laterTarget = laterOld
local function replaceLaterTarget()
    laterTarget = laterNew
    return laterNew
end
laterTarget.x, replaceLaterTarget().x, laterTarget = 1, 2, laterOld
assert(laterOld.x == nil, 'conflict copy must happen after later target expressions')
assert(laterNew.x == 1, 'earlier target must use the copied table')
assert(laterTarget == laterOld, 'direct target must still be assigned')

local numericTarget
local function replaceNumericTarget()
    numericTarget = {}
    return 'v'
end
local function assignNumericLiteralTarget()
    numericTarget = { old = true }
    numericTarget[1] = replaceNumericTarget()
end
assignNumericLiteralTarget()
if _VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3' then
    assert(numericTarget[1] == 'v', 'dynamic upvalue tables are deferred before Lua 5.4')
else
    assert(numericTarget[1] == nil, 'non-string literal keys snapshot upvalue tables')
end

local stringTarget
local function replaceStringTarget()
    stringTarget = {}
    return 'v'
end
local function assignStringLiteralTarget()
    stringTarget = { old = true }
    stringTarget['x'] = replaceStringTarget()
end
assignStringLiteralTarget()
if _VERSION == 'Lua 5.1' then
    assert(stringTarget.x == nil, 'Lua 5.1 snapshots upvalue tables')
else
    assert(stringTarget.x == 'v', 'string literal keys keep deferred upvalue tables')
end

local live = {}
local replacement = {}
local function changeLive()
    live = replacement
    return 'live'
end
live.x = changeLive()
assert(
    live == replacement and replacement.x == 'live',
    'unconflicted local table reads at store time'
)

local liveKey = {}
local changedKey = 'a'
local function changeKey()
    changedKey = 'b'
    return 7
end
liveKey[changedKey] = changeKey()
assert(liveKey.a == nil and liveKey.b == 7, 'unconflicted local key reads at store time')

print('parallel assignment preserved')
