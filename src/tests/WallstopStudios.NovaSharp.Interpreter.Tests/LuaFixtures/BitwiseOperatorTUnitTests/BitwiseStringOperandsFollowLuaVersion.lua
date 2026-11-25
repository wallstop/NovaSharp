-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local compile = loadstring or load
if _VERSION == "Lua 5.1" or _VERSION == "Lua 5.2" then
    assert(not compile("return '3' & 1"))
    assert(not compile("return ~'3'"))
    return
end

local is53 = _VERSION == "Lua 5.3"
local function checkError(expected, f, ...)
    local ok, err = pcall(f, ...)
    assert(not ok and err:find(expected, 1, true), tostring(err))
end
local typeError = "attempt to perform bitwise operation on a string value"
local integerError = "has no integer representation"

local operations = {
    { "&", "__band", 1, 1 },
    { "|", "__bor", 3, 3 },
    { "~", "__bxor", 2, 2 },
    { "<<", "__shl", 6, 8 },
    { ">>", "__shr", 1, 0 },
}
local stringMetatable = debug.getmetatable("")
for _, case in ipairs(operations) do
    local token, field, result, reversed = case[1], case[2], case[3], case[4]
    local f = assert(compile("return function(a, b) return a " .. token .. " b end"))()
    assert(f(3, 1.0) == result and f(1, 3) == reversed)
    assert(f(3, -0.0) == f(3, 0))
    checkError(typeError, f, "text", 1)
    checkError(typeError, f, 1, "text")
    checkError(is53 and integerError or typeError, f, "3.5", 1)
    checkError(typeError, f, 1.5, "text")
    checkError(integerError, f, 1, 3.5)
    if is53 then
        assert(f("0x3", 1) == result and f(1, "0x3") == reversed)
    else
        checkError(typeError, f, "3", 1)
        checkError(typeError, f, 1, "3")
        checkError(typeError, f, "3", "1")
    end

    local old = rawget(stringMetatable, field)
    local calls = 0
    stringMetatable[field] = function(a, b)
        calls = calls + 1
        return type(a) == "string" and 79 or 89
    end
    assert(f("3", 1) == (is53 and result or 79))
    assert(f(1, "3") == (is53 and reversed or 89))
    assert(f("text", 1) == 79 and f(1, "text") == 89)
    assert(calls == (is53 and 2 or 4))
    stringMetatable[field] = old
end

local shiftLeft, shiftRight = assert(compile([[
    return function(n) return 1 << n end, function(n) return 1 >> n end
]]))()
for _, n in ipairs({ math.mininteger, math.maxinteger, -64, 64 }) do
    assert(shiftLeft(n) == 0 and shiftRight(n) == 0)
end
assert(shiftLeft(-63) == 0 and shiftRight(-63) == math.mininteger)
if is53 then
    local minHex = "0x8000000000000000"
    assert(shiftLeft(minHex) == 0 and shiftRight(minHex) == 0)
end

local bnot = assert(compile("return function(a) return ~a end"))()
assert(bnot(3.0) == -4 and bnot(-0.0) == -1)
checkError(integerError, bnot, 3.5)
checkError(typeError, bnot, "text")
for _, numeral in ipairs({ "3", "0x3" }) do
    local nbsp = string.char(160)
    checkError(typeError, bnot, nbsp .. numeral)
    checkError(typeError, bnot, numeral .. nbsp)
end
checkError(is53 and integerError or typeError, bnot, "3.5")
local constantBnot = assert(compile("return ~'0x3'"))
if is53 then
    assert(constantBnot() == -4)
    assert(bnot("-0.0") == -1 and bnot("-0x0p0") == -1 and bnot("-1e-9999") == -1)
    assert(bnot("9223372036854775807") == math.mininteger)
else
    checkError(typeError, constantBnot)
end

local old = rawget(stringMetatable, "__bnot")
stringMetatable.__bnot = function(a, b)
    assert(rawequal(a, b))
    return 99
end
assert(bnot("3") == (is53 and -4 or 99))
assert(bnot("text") == 99)
stringMetatable.__bnot = old

local tagged = setmetatable({}, { __name = "Tagged" })
checkError("attempt to perform bitwise operation on a Tagged value", bnot, tagged)
local band = assert(compile("return function(a, b) return a & b end"))()
checkError("attempt to perform bitwise operation on a Tagged value", band, 1.5, tagged)

local originalAnd = rawget(stringMetatable, "__band")
stringMetatable.__band = coroutine.yield
local co = coroutine.create(function()
    local function verifyOperatorResult(...)
        assert(select("#", ...) == 1 and ... == 99)
    end
    verifyOperatorResult(band("text", 1))
    return coroutine.yield("ordinary")
end)
local ok, lhs, rhs = coroutine.resume(co)
assert(ok and lhs == "text" and rhs == 1)
assert(coroutine.status(co) == "suspended")
local resumed, result = coroutine.resume(co, 99, 100)
assert(resumed and result == "ordinary")
local function verifyOrdinaryYield(...)
    local finished, a, b = ...
    assert(finished and a == 11 and b == 12 and select("#", ...) == 3)
end
verifyOrdinaryYield(coroutine.resume(co, 11, 12))
assert(coroutine.status(co) == "dead")
stringMetatable.__band = originalAnd

local originalNil = debug.getmetatable(nil)
debug.setmetatable(nil, { __band = function(...)
    local a, b = ...
    assert(select("#", ...) == 2)
    assert((a == nil and b == "3") or (a == "3" and b == nil))
    return 42
end })
local left, right = assert(compile([[
    local function noResults() end
    return noResults() & '3', '3' & noResults()
]]))()
assert(left == 42 and right == 42)
debug.setmetatable(nil, originalNil)
