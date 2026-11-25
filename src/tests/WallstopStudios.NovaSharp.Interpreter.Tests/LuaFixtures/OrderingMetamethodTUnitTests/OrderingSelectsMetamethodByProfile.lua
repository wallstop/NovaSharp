-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local is51 = _VERSION == 'Lua 5.1'
local is55 = _VERSION == 'Lua 5.5'
local trace = ''

local function lt(a, b)
    trace = trace .. 'lt:' .. a.label .. b.label
    return false, true
end

local function le(a, b)
    trace = trace .. 'le:' .. a.label .. b.label
    return nil, true
end

local function unexpected()
    trace = trace .. 'unexpected'
    return true
end

local function less(a, b) return a < b end
local function lessEqual(a, b) return a <= b end
local function check(op, leftMeta, rightMeta, success, value, expectedTrace)
    local a = setmetatable({label = 'a'}, leftMeta)
    local b = setmetatable({label = 'b'}, rightMeta)
    trace = ''
    local ok, result = pcall(op, a, b)
    assert(ok == success, 'dispatch success: ' .. tostring(result))
    if success then
        assert(result == value, 'boolean comparison result')
    else
        assert(result:find('attempt to compare two table values', 1, true), 'ordering error category')
    end
    assert(trace == expectedTrace, 'hook trace: ' .. trace)
end

-- The two metatables can differ as long as the selected hook is the same value.
local left = {__lt = lt, __le = le}
local right = {__lt = lt, __le = le}
check(less, left, right, true, false, 'lt:ab')
check(lessEqual, left, right, true, false, 'le:ab')

check(less, {}, {}, false, nil, '')
check(lessEqual, {}, {}, false, nil, '')
check(less, left, {}, not is51, false, is51 and '' or 'lt:ab')
check(less, {}, right, not is51, false, is51 and '' or 'lt:ab')
check(lessEqual, left, {}, not is51, false, is51 and '' or 'le:ab')
check(lessEqual, {}, right, not is51, false, is51 and '' or 'le:ab')
check(less, left, {__lt = unexpected}, not is51, false, is51 and '' or 'lt:ab')
check(lessEqual, left, {__le = unexpected}, not is51, false, is51 and '' or 'le:ab')

-- A missing or mismatched __le triggers __lt with reversed operands until Lua 5.5.
check(lessEqual, {__lt = lt}, {__lt = lt}, not is55, true, is55 and '' or 'lt:ba')
check(lessEqual, {}, {__lt = lt}, not is51 and not is55, true,
    (is51 or is55) and '' or 'lt:ba')
check(lessEqual, left, {__le = unexpected, __lt = lt}, true, is51,
    is51 and 'lt:ba' or 'le:ab')

local function call(_, a, b)
    trace = trace .. 'call:' .. a.label .. b.label
    return true
end
local function equal()
    trace = trace .. 'unexpected equality hook'
    return true
end
local callableMeta = {__call = call, __eq = equal}
local callable = setmetatable({}, callableMeta)
check(less, {__lt = callable}, {__lt = callable}, true, true, 'call:ab')
check(less, {__lt = callable}, {__lt = setmetatable({}, callableMeta)}, not is51, true,
    is51 and '' or 'call:ab')

local function empty() end
local function nilHook(...)
    local a, b = ...
    return select('#', ...) == 2 and a == nil and b == nil
end
debug.setmetatable(nil, {__lt = nilHook, __le = nilHook})
assert(empty() < nil and nil <= empty(), 'empty results compare as Lua nil')
assert(empty() <= empty(), 'two empty results contribute two nil operands')

if is51 then
    local native = print
    local function functionHook(a, b)
        return rawequal(a, native) and rawequal(b, empty)
    end
    debug.setmetatable(empty, {__lt = functionHook, __le = functionHook})
    assert(native < empty and native <= empty, 'native and Lua functions share a Lua type')
    assert(not (empty < native) and not (native <= native), 'function metatable operand order')
end

local yieldError = 'attempt to yield across metamethod/C-call boundary'
local function checkYield(op, hookName, invert)
    local metatable = {[hookName] = coroutine.yield}
    local a, b = setmetatable({}, metatable), setmetatable({}, metatable)
    local function compare() return op(a, b) end
    local thread = coroutine.create(compare)
    local ok, first, second = coroutine.resume(thread)
    if is51 then
        assert(not ok and first:find(yieldError, 1, true), hookName .. ': Lua 5.1 yield boundary')
        local recovered = coroutine.create(function()
            local success, message = pcall(compare)
            assert(not success and message:find(yieldError, 1, true), 'protected yield boundary')
            local resumed = coroutine.yield('recovered')
            return resumed
        end)
        local success, value = coroutine.resume(recovered)
        assert(success and value == 'recovered', hookName .. ': recover after protected hook')
        success, value = coroutine.resume(recovered, 'finished')
        assert(success and value == 'finished', hookName .. ': no stale comparison state')
    elseif invert and is55 then
        assert(not ok and first:find('attempt to compare', 1, true), 'Lua 5.5 skips fallback')
    else
        assert(ok and rawequal(first, invert and b or a) and rawequal(second, invert and a or b),
            hookName .. ': yield operands')
        assert(coroutine.status(thread) == 'suspended', hookName .. ': suspended hook')
        local resumed, result = coroutine.resume(thread, true)
        assert(resumed and result == not invert, hookName .. ': resumed comparison result')
    end
    assert(coroutine.status(thread) == 'dead', hookName .. ': finished hook')
    assert(not coroutine.resume(thread), hookName .. ': finished hook rejects resume')
end

checkYield(less, '__lt', false)
checkYield(lessEqual, '__le', false)
checkYield(lessEqual, '__lt', true)
