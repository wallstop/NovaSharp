-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test math.modf behavior with normal numbers for baseline verification
-- math.modf(x) returns the integer part and fractional part of x
-- Property: int_part + frac_part == x (for finite numbers)

-- Helper to check if a value is NaN
local function isnan(x)
    return x ~= x
end

-- Helper to check if a value is negative zero
local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

-- Helper to compare floating point with tolerance
local function approx_equal(a, b, epsilon)
    epsilon = epsilon or 1e-10
    return math.abs(a - b) < epsilon
end

print("=== math.modf normal cases ===")

-- Test 1: math.modf(1.5) - positive with fractional part
print("Test 1: math.modf(1.5)")
local int_part, frac_part = math.modf(1.5)
assert(int_part == 1, "Integer part of 1.5 should be 1")
assert(frac_part == 0.5, "Fractional part of 1.5 should be 0.5")
assert(int_part + frac_part == 1.5, "Reconstruction: 1 + 0.5 == 1.5")
print("PASS: modf(1.5) = 1, 0.5")

-- Test 2: math.modf(-1.5) - negative with fractional part
print("Test 2: math.modf(-1.5)")
int_part, frac_part = math.modf(-1.5)
assert(int_part == -1, "Integer part of -1.5 should be -1")
assert(frac_part == -0.5, "Fractional part of -1.5 should be -0.5")
assert(int_part + frac_part == -1.5, "Reconstruction: -1 + -0.5 == -1.5")
print("PASS: modf(-1.5) = -1, -0.5")

-- Test 3: math.modf(2.5) - another positive case
print("Test 3: math.modf(2.5)")
int_part, frac_part = math.modf(2.5)
assert(int_part == 2, "Integer part of 2.5 should be 2")
assert(frac_part == 0.5, "Fractional part of 2.5 should be 0.5")
assert(int_part + frac_part == 2.5, "Reconstruction should hold")
print("PASS: modf(2.5) = 2, 0.5")

-- Test 4: math.modf(-2.5) - another negative case
print("Test 4: math.modf(-2.5)")
int_part, frac_part = math.modf(-2.5)
assert(int_part == -2, "Integer part of -2.5 should be -2")
assert(frac_part == -0.5, "Fractional part of -2.5 should be -0.5")
assert(int_part + frac_part == -2.5, "Reconstruction should hold")
print("PASS: modf(-2.5) = -2, -0.5")

-- Test 5: math.modf(3.0) - whole number (positive)
print("Test 5: math.modf(3.0)")
int_part, frac_part = math.modf(3.0)
assert(int_part == 3, "Integer part of 3.0 should be 3")
assert(frac_part == 0, "Fractional part of 3.0 should be 0")
assert(int_part + frac_part == 3.0, "Reconstruction should hold")
print("PASS: modf(3.0) = 3, 0")

-- Test 6: math.modf(-3.0) - whole number (negative)
print("Test 6: math.modf(-3.0)")
int_part, frac_part = math.modf(-3.0)
assert(int_part == -3, "Integer part of -3.0 should be -3")
assert(frac_part == 0, "Fractional part of -3.0 should be 0")
-- Note: frac_part may be -0 for negative whole numbers
assert(int_part + frac_part == -3.0, "Reconstruction should hold")
print("PASS: modf(-3.0) = -3, 0")

-- Test 7: math.modf(0.5) - positive less than 1
print("Test 7: math.modf(0.5)")
int_part, frac_part = math.modf(0.5)
assert(int_part == 0, "Integer part of 0.5 should be 0")
assert(frac_part == 0.5, "Fractional part of 0.5 should be 0.5")
assert(int_part + frac_part == 0.5, "Reconstruction should hold")
print("PASS: modf(0.5) = 0, 0.5")

-- Test 8: math.modf(-0.5) - negative with magnitude less than 1
print("Test 8: math.modf(-0.5)")
int_part, frac_part = math.modf(-0.5)
assert(int_part == 0, "Integer part of -0.5 should be 0")
assert(frac_part == -0.5, "Fractional part of -0.5 should be -0.5")
-- Note: int_part may be -0 in some versions
assert(int_part + frac_part == -0.5, "Reconstruction should hold")
print("PASS: modf(-0.5) = 0, -0.5")

-- Test 9: math.modf(1.0) - exactly 1
print("Test 9: math.modf(1.0)")
int_part, frac_part = math.modf(1.0)
assert(int_part == 1, "Integer part of 1.0 should be 1")
assert(frac_part == 0, "Fractional part of 1.0 should be 0")
print("PASS: modf(1.0) = 1, 0")

-- Test 10: math.modf(-1.0) - exactly -1
print("Test 10: math.modf(-1.0)")
int_part, frac_part = math.modf(-1.0)
assert(int_part == -1, "Integer part of -1.0 should be -1")
assert(frac_part == 0, "Fractional part of -1.0 should be 0")
print("PASS: modf(-1.0) = -1, 0")

-- Test 11: math.modf with pi
print("Test 11: math.modf(math.pi)")
int_part, frac_part = math.modf(math.pi)
assert(int_part == 3, "Integer part of pi should be 3")
assert(approx_equal(frac_part, math.pi - 3), "Fractional part should be pi - 3")
assert(approx_equal(int_part + frac_part, math.pi), "Reconstruction should hold")
print("PASS: modf(pi) = 3, 0.14159...")

-- Test 12: math.modf(-math.pi)
print("Test 12: math.modf(-math.pi)")
int_part, frac_part = math.modf(-math.pi)
assert(int_part == -3, "Integer part of -pi should be -3")
assert(approx_equal(frac_part, -(math.pi - 3)), "Fractional part should be -(pi - 3)")
assert(approx_equal(int_part + frac_part, -math.pi), "Reconstruction should hold")
print("PASS: modf(-pi) = -3, -0.14159...")

-- Test 13: math.modf with larger numbers
print("Test 13: math.modf(123.456)")
int_part, frac_part = math.modf(123.456)
assert(int_part == 123, "Integer part of 123.456 should be 123")
assert(approx_equal(frac_part, 0.456), "Fractional part should be 0.456")
print("PASS: modf(123.456) = 123, 0.456")

-- Test 14: math.modf(-123.456)
print("Test 14: math.modf(-123.456)")
int_part, frac_part = math.modf(-123.456)
assert(int_part == -123, "Integer part of -123.456 should be -123")
assert(approx_equal(frac_part, -0.456), "Fractional part should be -0.456")
print("PASS: modf(-123.456) = -123, -0.456")

-- Test 15: Fractional part is always in range (-1, 1)
print("Test 15: Fractional part range check")
local test_values = {1.5, -1.5, 2.5, -2.5, 100.99, -100.99, 0.001, -0.001}
for _, v in ipairs(test_values) do
    int_part, frac_part = math.modf(v)
    assert(frac_part > -1 and frac_part < 1, 
           "Fractional part must be in range (-1, 1) for " .. v)
end
print("PASS: All fractional parts in valid range")

-- Test 16: Sign of fractional part matches sign of input
print("Test 16: Sign consistency")
int_part, frac_part = math.modf(1.5)
assert(frac_part > 0, "Fractional part of positive should be positive")
int_part, frac_part = math.modf(-1.5)
assert(frac_part < 0, "Fractional part of negative should be negative")
print("PASS: Sign consistency verified")

-- Test 17: Type checking
print("Test 17: Type checking")
int_part, frac_part = math.modf(1.5)
assert(type(int_part) == "number", "Integer part should be number")
assert(type(frac_part) == "number", "Fractional part should be number")
print("PASS: Types are correct")

-- Test 18: Very small fractional parts
print("Test 18: math.modf(1.0000001)")
int_part, frac_part = math.modf(1.0000001)
assert(int_part == 1, "Integer part should be 1")
assert(frac_part > 0 and frac_part < 0.001, "Fractional part should be tiny")
print("PASS: modf(1.0000001) handles tiny fractions")

-- Test 19: Large integer with small fraction
print("Test 19: math.modf(1000000.1)")
int_part, frac_part = math.modf(1000000.1)
assert(int_part == 1000000, "Integer part should be 1000000")
assert(approx_equal(frac_part, 0.1, 1e-6), "Fractional part should be ~0.1")
print("PASS: modf(1000000.1) handles large numbers")

-- Test 20: Reconstruction property across various inputs
print("Test 20: Reconstruction property")
local reconstruction_tests = {
    0.1, -0.1, 0.9, -0.9, 1.1, -1.1, 
    10.5, -10.5, 100.001, -100.001,
    math.pi, -math.pi, math.pi * 100, -math.pi * 100
}
for _, v in ipairs(reconstruction_tests) do
    int_part, frac_part = math.modf(v)
    local reconstructed = int_part + frac_part
    assert(approx_equal(reconstructed, v, 1e-10), 
           "Reconstruction failed for " .. v .. ": got " .. reconstructed)
end
print("PASS: Reconstruction property holds for all test cases")

print("")
print("=== All normal case tests passed ===")
