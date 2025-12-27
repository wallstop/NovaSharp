-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfSignPreservation
-- @compat-notes: math.modf sign behavior differs between Lua 5.2 and Lua 5.3+
-- Lua 5.2: Preserves signed zeros in both parts (uses copysign internally)
-- Lua 5.3+: Integer part loses sign info due to integer subtype, fractional uses +0 for whole numbers
-- Note: Lua 5.1 is excluded due to internal state bugs affecting signed zero behavior across calls

local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

local function is_positive_zero(x)
    return x == 0 and 1/x == math.huge
end

local function sign_str(x)
    if x == 0 then
        return is_negative_zero(x) and "-0" or "+0"
    elseif x > 0 then
        return "+"
    else
        return "-"
    end
end

-- Detect Lua version (5.3+ has integer subtype)
local is_lua53_plus = (function()
    local v = _VERSION:match("Lua (%d+%.%d+)")
    local major, minor = v:match("(%d+)%.(%d+)")
    return tonumber(major) > 5 or (tonumber(major) == 5 and tonumber(minor) >= 3)
end)()

local is_lua52 = _VERSION == "Lua 5.2"

print("Lua version: " .. _VERSION .. " (5.3+: " .. tostring(is_lua53_plus) .. ")")

-- Test 1: positive zero input FIRST (to avoid Lua 5.1 internal state quirk)
local pos_zero = 0.0
local int1, frac1 = math.modf(pos_zero)
print("math.modf(0) = " .. tostring(int1) .. " (" .. sign_str(int1) .. "), " .. tostring(frac1) .. " (" .. sign_str(frac1) .. ")")
assert(int1 == 0, "modf(0) int should be 0")
assert(frac1 == 0, "modf(0) frac should be 0")
assert(is_positive_zero(int1), "modf(0) int should be +0")
assert(is_positive_zero(frac1), "modf(0) frac should be +0")

-- Test 2: positive integer - fractional part should be +0 (all versions)
local int2, frac2 = math.modf(5)
print("math.modf(5) = " .. tostring(int2) .. ", " .. tostring(frac2) .. " (" .. sign_str(frac2) .. ")")
assert(int2 == 5, "modf(5) int should be 5")
assert(frac2 == 0, "modf(5) frac should be 0")
assert(is_positive_zero(frac2), "modf(5) frac should be +0")

-- Test 3: positive number with fractional part
local int3, frac3 = math.modf(2.5)
print("math.modf(2.5) = " .. tostring(int3) .. ", " .. tostring(frac3))
assert(int3 == 2, "modf(2.5) int should be 2")
assert(frac3 == 0.5, "modf(2.5) frac should be 0.5")

-- Test 4: very small positive number (0 < x < 1)
local int4, frac4 = math.modf(0.25)
print("math.modf(0.25) = " .. tostring(int4) .. " (" .. sign_str(int4) .. "), " .. tostring(frac4))
assert(int4 == 0, "modf(0.25) int should be 0")
assert(is_positive_zero(int4), "modf(0.25) int should be +0")
assert(frac4 == 0.25, "modf(0.25) frac should be 0.25")

-- Now test negative values (after all positive zero assertions are done)

-- Test 5: negative zero input
local neg_zero = -0.0
local int5, frac5 = math.modf(neg_zero)
print("math.modf(-0) = " .. tostring(int5) .. " (" .. sign_str(int5) .. "), " .. tostring(frac5) .. " (" .. sign_str(frac5) .. ")")
assert(int5 == 0, "modf(-0) int should be 0")
assert(frac5 == 0, "modf(-0) frac should be 0")
if is_lua53_plus then
    -- Lua 5.3+ returns +0 for both (integer subtype)
    assert(is_positive_zero(int5), "Lua 5.3+: modf(-0) int should be +0")
    assert(is_positive_zero(frac5), "Lua 5.3+: modf(-0) frac should be +0")
else
    -- Lua 5.2 preserves -0
    assert(is_negative_zero(int5), "Lua 5.2: modf(-0) int should be -0")
    assert(is_negative_zero(frac5), "Lua 5.2: modf(-0) frac should be -0")
end

-- Test 6: negative integer - fractional part sign differs by version
local int6, frac6 = math.modf(-5)
print("math.modf(-5) = " .. tostring(int6) .. ", " .. tostring(frac6) .. " (" .. sign_str(frac6) .. ")")
assert(int6 == -5, "modf(-5) int should be -5")
assert(frac6 == 0, "modf(-5) frac should be 0")
if is_lua53_plus then
    assert(is_positive_zero(frac6), "Lua 5.3+: modf(-5) frac should be +0")
else
    assert(is_negative_zero(frac6), "Lua 5.2: modf(-5) frac should be -0")
end

-- Test 7: negative number with fractional part
local int7, frac7 = math.modf(-2.5)
print("math.modf(-2.5) = " .. tostring(int7) .. ", " .. tostring(frac7))
assert(int7 == -2, "modf(-2.5) int should be -2")
assert(frac7 == -0.5, "modf(-2.5) frac should be -0.5")

-- Test 8: very small negative number (0 > x > -1)
local int8, frac8 = math.modf(-0.25)
print("math.modf(-0.25) = " .. tostring(int8) .. " (" .. sign_str(int8) .. "), " .. tostring(frac8))
assert(int8 == 0, "modf(-0.25) int should be 0")
assert(frac8 == -0.25, "modf(-0.25) frac should be -0.25")
if is_lua53_plus then
    assert(is_positive_zero(int8), "Lua 5.3+: modf(-0.25) int should be +0")
else
    assert(is_negative_zero(int8), "Lua 5.2: modf(-0.25) int should be -0")
end

print("PASS: math.modf sign preservation")
