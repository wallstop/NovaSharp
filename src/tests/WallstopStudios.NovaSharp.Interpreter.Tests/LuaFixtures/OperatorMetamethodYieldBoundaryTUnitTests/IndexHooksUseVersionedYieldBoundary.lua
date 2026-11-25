-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"
local function index(_, key)
    local value = coroutine.yield(key)
    return value
end
local function newindex(target, key, pending)
    assert(pending == "assignment")
    local value = coroutine.yield(key)
    rawset(target, key, value)
end
local operations = {
    { function(target) return target.field end, false },
    { function(target, key) return target[key] end, false },
    { function(target) target.field = "assignment"; return rawget(target, "field") end, true },
    { function(target, key) target[key] = "assignment"; return rawget(target, key) end, true },
}
for _, case in ipairs(operations) do
    local operation, writes = case[1], case[2]
    local target = setmetatable({}, { __index = index, __newindex = newindex })
    local co = coroutine.create(function() return operation(target, "field") end)
    local ok, value = coroutine.resume(co)
    assert(rawget(target, "field") == nil)
    if is51 then
        assert(not ok and value:find("attempt to yield across metamethod/C-call boundary", 1, true), value)
    else
        assert(ok and value == "field", value)
        assert(coroutine.status(co) == "suspended")
        ok, value = coroutine.resume(co, 42)
        assert(ok and value == 42, value)
        assert(rawget(target, "field") == (writes and 42 or nil))
    end
    assert(coroutine.status(co) == "dead")
end

local callable = setmetatable({}, { __call = function()
    return coroutine.yield("call")
end })
local co = coroutine.create(function() return callable() end)
local ok, yielded = coroutine.resume(co)
assert(ok and yielded == "call")
assert(coroutine.status(co) == "suspended")
local completed, first, second = coroutine.resume(co, 7, 8)
assert(completed and first == 7 and second == 8)
assert(coroutine.status(co) == "dead")
