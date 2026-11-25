-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"
local function verifyYield(operand, unary, expectedResult, ...)
    local function verifyResult(...)
        local resumed, value = ...
        assert(resumed and value == expectedResult and select("#", ...) == 2, value)
    end
    local co = coroutine.create(function() return unary(operand) end)
    local ok, first, second = coroutine.resume(co)
    if is51 then
        assert(not ok and string.find(first, "attempt to yield across metamethod/C-call boundary", 1, true), first)
    else
        assert(ok and rawequal(first, operand) and rawequal(second, operand))
        assert(coroutine.status(co) == "suspended")
        verifyResult(coroutine.resume(co, ...))
    end
    assert(coroutine.status(co) == "dead")
end

local operand = setmetatable({}, { __unm = coroutine.yield, __bnot = coroutine.yield })
verifyYield(operand, function(value) return -value end, 77, 77, 88)

local original = debug.getmetatable(true)
debug.setmetatable(true, { __len = coroutine.yield })
verifyYield(true, function(value) return #value end, nil)
debug.setmetatable(true, original)

if not is51 and _VERSION ~= "Lua 5.2" then
    verifyYield(operand, assert(load("return function(value) return ~value end"))(), false, false, 88)
end
