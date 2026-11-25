-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false

local function check(expected, ok, ...)
    assert(ok, (...))
    assert(select("#", ...) == 1 and (...) == expected)
end

local function helper()
    local count = select("#", coroutine.yield("inside hook"))
    assert(count == 2)
    return false, "extra"
end

local twoStages = setmetatable({}, { __add = function() return helper() end })
local function start(co, ...)
    local ok, first = coroutine.resume(co, ...)
    assert(ok and first ~= nil and coroutine.status(co) == "suspended")
end

local delayed = coroutine.create(function() return twoStages + twoStages end)
start(delayed)
check(false, coroutine.resume(delayed, "first", "second"))
assert(coroutine.status(delayed) == "dead")

local operators = {
    { "__index", function(a) return a.missing end },
    { "__add", function(a) return a + a end },
    { "__sub", function(a) return a - a end },
    { "__mul", function(a) return a * a end },
    { "__div", function(a) return a / a end },
    { "__mod", function(a) return a % a end },
    { "__pow", function(a) return a ^ a end },
    { "__concat", function(a) return a .. a end },
}
if _VERSION ~= "Lua 5.2" then
    operators[#operators + 1] = { "__idiv", assert(load("local a = ...; return a // a")) }
end
for _, operator in ipairs(operators) do
    local direct = setmetatable({}, { [operator[1]] = coroutine.yield })
    local immediate = coroutine.create(operator[2])
    start(immediate, direct)
    check(42, coroutine.resume(immediate, 42, 99))
end

local direct = setmetatable({}, { __add = coroutine.yield })
local immediateEmpty = coroutine.create(function() return direct + direct end)
start(immediateEmpty)
check(nil, coroutine.resume(immediateEmpty))

local equal = setmetatable({1}, { __eq = coroutine.yield })
local other = setmetatable({2}, debug.getmetatable(equal))
local equality = coroutine.create(function() return equal == other end)
start(equality)
check(false, coroutine.resume(equality, false, true))

local ordinary = coroutine.create(function() return coroutine.yield("ordinary") end)
start(ordinary)
local function checkOrdinary(...)
    local ok, one, two = ...
    assert(ok and one == 1 and two == 2 and select("#", ...) == 3)
end
checkOrdinary(coroutine.resume(ordinary, 1, 2))
