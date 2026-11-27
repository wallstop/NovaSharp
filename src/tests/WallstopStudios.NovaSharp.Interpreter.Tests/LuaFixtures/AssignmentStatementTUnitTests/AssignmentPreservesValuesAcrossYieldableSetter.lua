-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false

local target = {}
local kept = 'before'
local proxy = setmetatable({}, {
    __newindex = function(table, key, value)
        coroutine.yield('setting', key, value)
        rawset(table, key, value)
        return 'discarded'
    end,
})
local suspended = coroutine.create(function()
    proxy.x, kept, target.y = 'first', false, 'third'
    return proxy.x, kept, target.y
end)

local ok, phase, key, value = coroutine.resume(suspended)
assert(ok and phase == 'setting' and key == 'x' and value == 'first', 'setter must yield')

local values = { coroutine.resume(suspended) }
assert(values[1] == true, 'coroutine must resume successfully')
assert(values[2] == 'first' and values[3] == false and values[4] == 'third', 'pending values')
assert(coroutine.status(suspended) == 'dead', 'coroutine must finish')
assert(proxy.x == 'first' and target.y == 'third', 'setter must store all values')

print('yieldable setter preserved')
