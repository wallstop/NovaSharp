-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.StringMetamethodFallsBackToOtherOperandMetamethod
-- @compat-notes: Lua 5.4+ string metamethods fall back to other operand's metamethod when coercion fails

-- Test: String + table uses table's __add metamethod, not string's
-- Reference: Lua 5.4 manual §3.4.3 - "If the conversion fails, the library calls the metamethod of the other operand"

local Cplx = {}
Cplx.mt = {}

function Cplx.new(re, im)
    local c = {}
    setmetatable(c, Cplx.mt)
    c.re = tonumber(re) or re
    c.im = tonumber(im) or im
    return c
end

function Cplx.mt.__add(a, b)
    if type(a) ~= 'table' then
        a = Cplx.new(a, 0)
    end
    if type(b) ~= 'table' then
        b = Cplx.new(b, 0)
    end
    return Cplx.new(a.re + b.re, a.im + b.im)
end

function Cplx.mt.__tostring(c)
    return '(' .. c.re .. ',' .. c.im .. ')'
end

local c1 = Cplx.new(1, 3)

-- String is first operand, but table's __add should be used
local result = tostring('-2' + c1)
assert(result == '(-1,3)', "'-2' + c1 should use table's __add, got: " .. result)

-- Number string + complex
result = tostring('3' + c1)
assert(result == '(4,3)', "'3' + c1 should work, got: " .. result)

-- Complex + string 
result = tostring(c1 + '5')
assert(result == '(6,3)', "c1 + '5' should work, got: " .. result)

print("String metamethod correctly falls back to table's metamethod")
return true
