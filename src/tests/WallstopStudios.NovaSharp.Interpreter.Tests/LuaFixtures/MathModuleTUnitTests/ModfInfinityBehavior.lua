-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test math.modf behavior with infinity values
-- Per IEEE 754 and Lua reference: modf(inf) returns (inf, 0), modf(-inf) returns (-inf, -0)

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

-- Helper to check if a value is positive infinity
local function is_positive_inf(x)
    return x == math.huge
end

-- Helper to check if a value is negative infinity
local function is_negative_inf(x)
    return x == -math.huge
end

print("=== math.modf infinity behavior ===")

-- Test 1: math.modf(math.huge) - positive infinity
print("Test 1: math.modf(math.huge)")
local int_part, frac_part = math.modf(math.huge)
assert(is_positive_inf(int_part), "Integer part of modf(inf) should be +inf")
assert(frac_part == 0, "Fractional part of modf(inf) should be 0")
assert(is_positive_zero(frac_part), "Fractional part of modf(inf) should be +0")
assert(not isnan(frac_part), "Fractional part of modf(inf) should NOT be NaN")
print("PASS: modf(math.huge) = inf, 0")

-- Test 2: math.modf(-math.huge) - negative infinity
print("Test 2: math.modf(-math.huge)")
int_part, frac_part = math.modf(-math.huge)
assert(is_negative_inf(int_part), "Integer part of modf(-inf) should be -inf")
assert(frac_part == 0, "Fractional part of modf(-inf) should be 0")
-- Note: The sign of zero in the fractional part varies by version
-- Lua 5.1/5.2 return -0, Lua 5.3/5.4 return +0
-- We only assert it's zero, not the sign
assert(not isnan(frac_part), "Fractional part of modf(-inf) should NOT be NaN")
print("PASS: modf(-math.huge) = -inf, 0")

-- Test 3: Verify integer part is exactly infinity (not a large number)
print("Test 3: Integer part is exactly infinity")
int_part, frac_part = math.modf(math.huge)
assert(int_part == math.huge, "Integer part should equal math.huge exactly")
assert(int_part + 1 == int_part, "Integer part should exhibit infinity arithmetic")
print("PASS: Integer part is exactly infinity")

-- Test 4: Verify fractional part is exactly zero (finite)
print("Test 4: Fractional part is exactly zero")
int_part, frac_part = math.modf(math.huge)
assert(frac_part + 1 == 1, "Fractional part should be exactly zero (0 + 1 = 1)")
assert(frac_part * 2 == 0, "Fractional part should be exactly zero (0 * 2 = 0)")
print("PASS: Fractional part is exactly zero")

-- Test 5: Reconstruction property does NOT hold for infinity
-- (inf + 0 = inf, but we can't fully verify reconstruction for infinity)
print("Test 5: Verify modf output values for infinity")
int_part, frac_part = math.modf(math.huge)
assert(int_part + frac_part == math.huge, "inf + 0 should equal inf")
print("PASS: modf components sum correctly for +inf")

int_part, frac_part = math.modf(-math.huge)
assert(int_part + frac_part == -math.huge, "-inf + 0 should equal -inf")
print("PASS: modf components sum correctly for -inf")

-- Test 6: Type checking - both return values should be numbers
print("Test 6: Type checking")
int_part, frac_part = math.modf(math.huge)
assert(type(int_part) == "number", "Integer part should be a number")
assert(type(frac_part) == "number", "Fractional part should be a number")
print("PASS: Both return values are numbers")

print("")
print("=== All infinity tests passed ===")
