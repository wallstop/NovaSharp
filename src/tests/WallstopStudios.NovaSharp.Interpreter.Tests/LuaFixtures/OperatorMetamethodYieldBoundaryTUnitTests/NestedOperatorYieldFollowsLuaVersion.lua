-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"
local function verifyOperation(label, operation, left, right, resumeValue, expected)
    local co = coroutine.create(function() return operation(left, right) end)
    local ok, result = coroutine.resume(co)
    if is51 then
        assert(not ok and result:find("attempt to yield across metamethod/C-call boundary", 1, true), label)
    else
        assert(ok and result == "paused at hook", label)
        assert(coroutine.status(co) == "suspended", label)
        local completed, value = coroutine.resume(co, resumeValue)
        assert(completed and value == expected, label)
    end
    assert(coroutine.status(co) == "dead", label)
end

local function tailHook()
    return coroutine.yield("paused at hook")
end
local function nestedHook()
    local result = tailHook()
    return result
end
local operations = {
    { "__add", function(a, b) return a + b end, 42, 42 },
    { "__sub", function(a, b) return a - b end, 42, 42 },
    { "__mul", function(a, b) return a * b end, 42, 42 },
    { "__div", function(a, b) return a / b end, 42, 42 },
    { "__mod", function(a, b) return a % b end, 42, 42 },
    { "__pow", function(a, b) return a ^ b end, 42, 42 },
    { "__unm", function(a) return -a end, 42, 42 },
    { "__concat", function(a, b) return a .. b end, "joined", "joined" },
    { "__eq", function(a, b) return a == b end, "truthy", true },
    { "__lt", function(a, b) return a < b end, false, false },
    { "__le", function(a, b) return a <= b end, "truthy", true },
}
if _VERSION ~= "Lua 5.5" then
    operations[#operations + 1] = { "__lt", function(a, b) return a <= b end, "truthy", false }
end
for _, hook in ipairs({ tailHook, nestedHook }) do
    for _, case in ipairs(operations) do
        local meta = { [case[1]] = hook }
        local left, right = setmetatable({}, meta), setmetatable({}, meta)
        verifyOperation(case[1], case[2], left, right, case[3], case[4])
    end
end

local original = debug.getmetatable(true)
debug.setmetatable(true, { __len = nestedHook })
verifyOperation("__len", function(a) return #a end, true, nil, 42, 42)
debug.setmetatable(true, original)

local direct = setmetatable({}, { __add = coroutine.yield })
local co = coroutine.create(function() return direct + direct end)
local ok, first, second = coroutine.resume(co)
if is51 then
    assert(not ok and first:find("attempt to yield across metamethod/C-call boundary", 1, true), first)
else
    assert(ok and rawequal(first, direct) and rawequal(second, direct))
    assert(coroutine.status(co) == "suspended")
    ok, first = coroutine.resume(co, 42)
    assert(ok and first == 42)
end
assert(coroutine.status(co) == "dead")
