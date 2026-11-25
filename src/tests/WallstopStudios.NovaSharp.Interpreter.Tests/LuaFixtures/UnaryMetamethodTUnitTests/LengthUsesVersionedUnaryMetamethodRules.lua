-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local is51 = _VERSION == "Lua 5.1"

local function verifyLengthError(operand, category)
    local ok, err = pcall(function() return #operand end)
    assert(not ok and string.find(err, category, 1, true), err)
end

verifyLengthError(true, "attempt to get length of")
local originalStringMeta = debug.getmetatable("")
debug.setmetatable("", { __len = function() error("string length called __len") end })
assert(#"abc" == 3 and #"" == 0)
debug.setmetatable("", originalStringMeta)

local calls = 0
local function lengthHook(operand)
    return function(...)
        calls = calls + 1
        local first, second = ...
        assert(select("#", ...) == 2)
        assert(rawequal(first, operand))
        if is51 then
            assert(second == nil)
        else
            assert(rawequal(second, operand))
        end
        return 7
    end
end

local values = { 1 }
setmetatable(values, { __len = lengthHook(values) })
if is51 then
    assert(#values == 1 and calls == 0)
else
    assert(#values == 7 and calls == 1)
end

setmetatable(values, {
    __len = nil,
    __index = function() error("table length called __index") end,
})
assert(#values == 1)

local function verifyTypeMetatable(operand)
    local original = debug.getmetatable(operand)
    local before = calls
    debug.setmetatable(operand, { __len = lengthHook(operand) })
    assert(#operand == 7 and calls == before + 1)
    debug.setmetatable(operand, original)
end

verifyTypeMetatable(true)
verifyTypeMetatable(nil)
verifyTypeMetatable(coroutine.create(function() end))

local function noResults() end
local nilMetatable = debug.getmetatable(nil)
debug.setmetatable(nil, { __len = lengthHook(nil) })
assert(#noResults() == 7)
debug.setmetatable(nil, nilMetatable)

verifyLengthError(true, "attempt to get length of")
