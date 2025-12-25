-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfNegativeZeroPreservation
-- @compat-notes: math.modf preserves negative zero for fractional part of negative integers

-- Helper functions to detect signed zeros
local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

local function is_positive_zero(x)
    return x == 0 and 1/x == math.huge
end

local function zero_sign_str(x)
    if x ~= 0 then return tostring(x) end
    return is_negative_zero(x) and "-0" or "+0"
end

-- Test cases: {input, expected_int, expected_frac_is_negative_zero}
local test_cases = {
    -- Negative integers: fractional part should be -0
    {-5, -5, true},
    {-1, -1, true},
    {-100, -100, true},
    {-1000000, -1000000, true},
    
    -- Positive integers: fractional part should be +0
    {5, 5, false},
    {1, 1, false},
    {100, 100, false},
    {1000000, 1000000, false},
    
    -- Zero: fractional part should be +0 (positive zero)
    {0, 0, false},
}

local all_passed = true

for _, tc in ipairs(test_cases) do
    local input = tc[1]
    local expected_int = tc[2]
    local expected_neg_zero = tc[3]
    
    local int_part, frac_part = math.modf(input)
    
    -- Verify integer part
    local int_ok = (int_part == expected_int)
    
    -- Verify fractional part value is 0
    local frac_is_zero = (frac_part == 0)
    
    -- Verify fractional part sign
    local frac_sign_ok
    if expected_neg_zero then
        frac_sign_ok = is_negative_zero(frac_part)
    else
        frac_sign_ok = is_positive_zero(frac_part)
    end
    
    local test_passed = int_ok and frac_is_zero and frac_sign_ok
    
    if test_passed then
        print("PASS: math.modf(" .. input .. ") = " .. int_part .. ", " .. zero_sign_str(frac_part))
    else
        print("FAIL: math.modf(" .. input .. ")")
        print("  Expected: " .. expected_int .. ", " .. (expected_neg_zero and "-0" or "+0"))
        print("  Got: " .. int_part .. ", " .. zero_sign_str(frac_part))
        if not int_ok then
            print("  (integer part mismatch)")
        end
        if not frac_is_zero then
            print("  (fractional part not zero: " .. frac_part .. ")")
        end
        if not frac_sign_ok then
            print("  (fractional part sign mismatch: expected " .. 
                  (expected_neg_zero and "-0" or "+0") .. 
                  ", got " .. zero_sign_str(frac_part) .. ")")
        end
        all_passed = false
    end
end

if all_passed then
    print("PASS: All math.modf negative zero tests passed")
else
    error("FAIL: Some math.modf negative zero tests failed")
end
