-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"
local function isBoundaryError(err)
    return type(err) == "string" and err:find("attempt to yield across metamethod/C-call boundary", 1, true)
end

local child = coroutine.create(function() coroutine.yield("child") end)
local function hook()
    local ok, value = coroutine.resume(child)
    assert(ok and value == "child" and coroutine.status(child) == "suspended")
    if is51 then
        local protected, err = pcall(function() coroutine.yield("caught within hook") end)
        assert(not protected and isBoundaryError(err), err)
    end
    return coroutine.yield("hook")
end

local operand = setmetatable({}, { __add = hook })
local co = coroutine.create(function()
    local function evaluate()
        return operand + operand
    end
    local function check(sentinel, ok, value)
        assert(sentinel == "sentinel")
        if is51 then
            assert(not ok and isBoundaryError(value), value)
        else
            assert(ok and value == 42, value)
        end
    end
    check("sentinel", pcall(evaluate))
    local first, second = coroutine.yield("recovered")
    return first, second
end)

local ok, value = coroutine.resume(co)
if not is51 then
    assert(ok and value == "hook", value)
    assert(coroutine.status(co) == "suspended")
    ok, value = coroutine.resume(co, 42)
end
assert(ok and value == "recovered", value)
assert(coroutine.status(co) == "suspended")
local finished, first, second = coroutine.resume(co, 7, 8)
assert(finished and first == 7 and second == 8)
assert(coroutine.status(co) == "dead")
assert(coroutine.resume(child) and coroutine.status(child) == "dead")
