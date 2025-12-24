-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfBasicCases
-- @compat-notes: math.modf basic behavior for normal numeric values

local function approx_equal(a, b, epsilon)
    epsilon = epsilon or 1e-10
    return math.abs(a - b) < epsilon
end

-- Test 1: positive number with fractional part
local int1, frac1 = math.modf(3.5)
assert(int1 == 3, "modf(3.5) int should be 3, got: " .. tostring(int1))
assert(approx_equal(frac1, 0.5), "modf(3.5) frac should be 0.5, got: " .. tostring(frac1))
print("math.modf(3.5) = " .. int1 .. ", " .. frac1)

-- Test 2: negative number with fractional part
local int2, frac2 = math.modf(-3.5)
assert(int2 == -3, "modf(-3.5) int should be -3, got: " .. tostring(int2))
assert(approx_equal(frac2, -0.5), "modf(-3.5) frac should be -0.5, got: " .. tostring(frac2))
print("math.modf(-3.5) = " .. int2 .. ", " .. frac2)

-- Test 3: positive integer (no fractional part)
local int3, frac3 = math.modf(5)
assert(int3 == 5, "modf(5) int should be 5, got: " .. tostring(int3))
assert(frac3 == 0, "modf(5) frac should be 0, got: " .. tostring(frac3))
print("math.modf(5) = " .. int3 .. ", " .. frac3)

-- Test 4: negative integer (no fractional part)
local int4, frac4 = math.modf(-5)
assert(int4 == -5, "modf(-5) int should be -5, got: " .. tostring(int4))
assert(frac4 == 0, "modf(-5) frac should be 0, got: " .. tostring(frac4))
print("math.modf(-5) = " .. int4 .. ", " .. frac4)

-- Test 5: positive zero
local int5, frac5 = math.modf(0)
assert(int5 == 0, "modf(0) int should be 0, got: " .. tostring(int5))
assert(frac5 == 0, "modf(0) frac should be 0, got: " .. tostring(frac5))
print("math.modf(0) = " .. int5 .. ", " .. frac5)

-- Test 6: small positive number (0 < x < 1)
local int6, frac6 = math.modf(0.75)
assert(int6 == 0, "modf(0.75) int should be 0, got: " .. tostring(int6))
assert(approx_equal(frac6, 0.75), "modf(0.75) frac should be 0.75, got: " .. tostring(frac6))
print("math.modf(0.75) = " .. int6 .. ", " .. frac6)

-- Test 7: small negative number (-1 < x < 0)
local int7, frac7 = math.modf(-0.75)
assert(int7 == 0, "modf(-0.75) int should be 0, got: " .. tostring(int7))
assert(approx_equal(frac7, -0.75), "modf(-0.75) frac should be -0.75, got: " .. tostring(frac7))
print("math.modf(-0.75) = " .. int7 .. ", " .. frac7)

-- Test 8: large number
local int8, frac8 = math.modf(1234567.89)
assert(int8 == 1234567, "modf(1234567.89) int should be 1234567, got: " .. tostring(int8))
assert(approx_equal(frac8, 0.89, 1e-6), "modf(1234567.89) frac should be ~0.89, got: " .. tostring(frac8))
print("math.modf(1234567.89) = " .. int8 .. ", " .. frac8)

-- Test 9: verify int + frac == original
local original = 42.125
local int9, frac9 = math.modf(original)
assert(approx_equal(int9 + frac9, original), "int + frac should equal original")
print("math.modf(" .. original .. ") = " .. int9 .. ", " .. frac9 .. " (sum = " .. (int9 + frac9) .. ")")

print("PASS: math.modf basic cases")
