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
