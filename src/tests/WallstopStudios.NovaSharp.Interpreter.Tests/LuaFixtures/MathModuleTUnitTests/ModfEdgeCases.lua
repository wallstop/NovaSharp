-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test math.modf behavior with edge cases: NaN, zeros, subnormals, and extreme values

-- Helper to check if a value is NaN
local function isnan(x)
    return x ~= x
end

-- Helper to check if a value is positive zero
local function is_positive_zero(x)
    return x == 0 and 1/x == math.huge
end

-- Helper to check if a value is negative zero
local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

print("=== math.modf edge cases ===")

-- Test 1: math.modf(0/0) - NaN
print("Test 1: math.modf(NaN)")
local nan = 0/0
local int_part, frac_part = math.modf(nan)
assert(isnan(int_part), "Integer part of modf(NaN) should be NaN")
assert(isnan(frac_part), "Fractional part of modf(NaN) should be NaN")
print("PASS: modf(NaN) = nan, nan")

-- Test 2: math.modf(0) - positive zero
print("Test 2: math.modf(0)")
int_part, frac_part = math.modf(0)
assert(int_part == 0, "Integer part of modf(0) should be 0")
assert(frac_part == 0, "Fractional part of modf(0) should be 0")
-- Both parts should be positive zero
assert(is_positive_zero(int_part), "Integer part of modf(0) should be +0")
assert(is_positive_zero(frac_part), "Fractional part of modf(0) should be +0")
print("PASS: modf(0) = 0, 0")

-- Test 3: math.modf(-0) - negative zero
-- Note: Behavior varies by Lua version
-- Lua 5.1: returns 0, 0 (both positive)
-- Lua 5.2+: returns -0, -0 (both negative)
print("Test 3: math.modf(-0)")
local neg_zero = -0.0
int_part, frac_part = math.modf(neg_zero)
assert(int_part == 0, "Integer part of modf(-0) should be 0")
assert(frac_part == 0, "Fractional part of modf(-0) should be 0")
-- We don't assert the sign since it varies by version
print("PASS: modf(-0) = 0, 0 (sign may vary by version)")

-- Test 4: math.modf(1e308) - very large number (near max double)
print("Test 4: math.modf(1e308)")
int_part, frac_part = math.modf(1e308)
assert(int_part == 1e308, "Integer part of modf(1e308) should be 1e308")
assert(frac_part == 0, "Fractional part of modf(1e308) should be 0")
assert(not isnan(int_part), "Integer part should not be NaN")
assert(not isnan(frac_part), "Fractional part should not be NaN")
print("PASS: modf(1e308) = 1e308, 0")

-- Test 5: math.modf(-1e308) - very large negative number
print("Test 5: math.modf(-1e308)")
int_part, frac_part = math.modf(-1e308)
assert(int_part == -1e308, "Integer part of modf(-1e308) should be -1e308")
assert(frac_part == 0, "Fractional part of modf(-1e308) should be 0")
print("PASS: modf(-1e308) = -1e308, 0")

-- Test 6: math.modf(1e-308) - very small number (subnormal range)
print("Test 6: math.modf(1e-308)")
int_part, frac_part = math.modf(1e-308)
assert(int_part == 0, "Integer part of modf(1e-308) should be 0")
assert(frac_part == 1e-308, "Fractional part of modf(1e-308) should be 1e-308")
assert(is_positive_zero(int_part), "Integer part should be +0")
print("PASS: modf(1e-308) = 0, 1e-308")

-- Test 7: math.modf(-1e-308) - very small negative number
print("Test 7: math.modf(-1e-308)")
int_part, frac_part = math.modf(-1e-308)
assert(int_part == 0, "Integer part of modf(-1e-308) should be 0")
assert(frac_part == -1e-308, "Fractional part of modf(-1e-308) should be -1e-308")
print("PASS: modf(-1e-308) = 0, -1e-308")

-- Test 8: Smallest positive subnormal
print("Test 8: math.modf with smallest subnormal")
local smallest = 2.2250738585072014e-308 -- Smallest normal
local subnormal = smallest / 2 -- A subnormal value
int_part, frac_part = math.modf(subnormal)
assert(int_part == 0, "Integer part of subnormal should be 0")
assert(frac_part == subnormal, "Fractional part should equal the subnormal")
print("PASS: modf(subnormal) = 0, subnormal")

-- Test 9: NaN propagation - operations on modf(NaN) results
print("Test 9: NaN propagation")
int_part, frac_part = math.modf(0/0)
assert(isnan(int_part + 1), "NaN + 1 should be NaN")
assert(isnan(frac_part * 2), "NaN * 2 should be NaN")
print("PASS: NaN propagates correctly")

-- Test 10: Type checking for edge cases
print("Test 10: Type checking")
int_part, frac_part = math.modf(0/0)
assert(type(int_part) == "number", "Integer part of NaN should be number type")
assert(type(frac_part) == "number", "Fractional part of NaN should be number type")
print("PASS: All return types are numbers")

print("")
print("=== All edge case tests passed ===")
