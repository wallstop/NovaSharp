-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local function many() return 7, 8, 9 end
local function none() end
local function tail() return many() end
local function nilFirst() return nil, 8 end

local function check(expected, ...)
    assert(select("#", ...) == 1, "operator result had wrong arity")
    assert((...) == expected, "operator result had wrong first value")
end

local meta = {
    __unm = many,
    __add = tail,
    __index = tail,
    __call = many,
    __sub = nilFirst,
    __concat = none,
    __len = many,
    __bnot = none,
    __band = many,
    __lt = many,
    __eq = none,
}
local a, b = setmetatable({}, meta), setmetatable({}, meta)
check(7, -a)
check(7, a + b)
check(7, a.missing)
check(nil, setmetatable({}, { __index = none }).missing)
assert(select("#", a()) == 3)
check(nil, a - b)
check(nil, a .. b)
check(true, a < b)
check(false, a == b)
assert((select("#", many())) == 3 and (select("#", none())) == 0)

local function result() return a + b end
check(7, result())
local x, y = -a
assert(x == 7 and y == nil)
local list = { 1, -a }
assert(list[1] == 1 and list[2] == 7 and list[3] == nil)

if _VERSION ~= "Lua 5.1" then
    check(7, #a)
end
if _VERSION ~= "Lua 5.1" and _VERSION ~= "Lua 5.2" then
    local bits = assert(load("return function(check, a) check(nil, ~a); check(7, a & a) end"))()
    bits(check, a)
end

-- This native hook tail-calls its first argument. Its protected continuation
-- returns a status plus results (or an error), of which the operator takes one.
debug.setmetatable(many, { __unm = pcall })
check(true, -many)
local function fail() error("hook failure") end
check(false, -fail)
debug.setmetatable(many, nil)
