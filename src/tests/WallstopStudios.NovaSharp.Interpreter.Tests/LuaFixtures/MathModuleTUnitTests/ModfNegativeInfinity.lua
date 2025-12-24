-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfNegativeInfinity
-- @compat-notes: math.modf(-inf) returns (-inf, 0) - fractional part is 0, not NaN
-- Note: Lua 5.1/5.2 return -0 for fractional part, Lua 5.3+ return +0 (integer subtype change)

local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

local function is_positive_zero(x)
    return x == 0 and 1/x == math.huge
end

-- Detect Lua version (5.3+ has integer subtype)
local is_lua53_plus = (function()
    local v = _VERSION:match("Lua (%d+%.%d+)")
    local major, minor = v:match("(%d+)%.(%d+)")
    return tonumber(major) > 5 or (tonumber(major) == 5 and tonumber(minor) >= 3)
end)()

-- Test math.modf with negative infinity
local int_part, frac_part = math.modf(-math.huge)

-- Integer part should be negative infinity
assert(int_part == -math.huge, "integer part should be -inf, got: " .. tostring(int_part))

-- Fractional part should be 0 (the key point: NOT NaN, despite inf - inf = NaN)
assert(frac_part == 0, "fractional part should be 0, got: " .. tostring(frac_part))

-- Sign of fractional part differs by version:
-- Lua 5.1/5.2: -0 (preserves sign via copysign)
-- Lua 5.3+: +0 (integer subtype loses sign)
if is_lua53_plus then
    assert(is_positive_zero(frac_part), "Lua 5.3+: fractional part should be +0")
else
    assert(is_negative_zero(frac_part), "Lua 5.1/5.2: fractional part should be -0")
end

print("math.modf(-math.huge) = " .. tostring(int_part) .. ", " .. tostring(frac_part))
print("fractional part sign: " .. (is_negative_zero(frac_part) and "-0" or "+0"))
print("Lua version: " .. _VERSION .. " (5.3+: " .. tostring(is_lua53_plus) .. ")")
print("PASS: math.modf negative infinity handling")
