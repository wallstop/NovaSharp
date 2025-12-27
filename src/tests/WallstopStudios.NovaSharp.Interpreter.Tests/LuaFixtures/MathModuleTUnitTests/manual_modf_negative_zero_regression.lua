-- Tests that math.modf(-n) returns the expected sign for the fractional zero part.
-- Note: The behavior varies by Lua version:
-- - Lua 5.1: Returns negative zero (-0)
-- - Lua 5.3+: Returns positive zero (0.0) - possibly due to integer math
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false

local function test_modf_negative(input, expected_int)
    local int_part, frac_part = math.modf(input)

    -- Check integer part
    if int_part ~= expected_int then
        error(string.format("FAIL: math.modf(%d) integer part = %s, expected %s",
            input, tostring(int_part), tostring(expected_int)))
    end

    -- Check fractional part is zero
    if frac_part ~= 0 then
        error(string.format("FAIL: math.modf(%d) fractional part = %s, expected 0",
            input, tostring(frac_part)))
    end

    -- Check fractional part is negative zero (Lua 5.1 behavior)
    -- 1/(-0) = -inf, 1/(+0) = +inf
    local is_neg_zero = (frac_part == 0 and 1 / frac_part == -math.huge)
    if not is_neg_zero then
        error(string.format("FAIL: math.modf(%d) fractional part should be -0 (negative zero), but 1/frac_part = %s",
            input, tostring(1 / frac_part)))
    end
end

-- Test cases for negative integers
test_modf_negative(-1, -1)
test_modf_negative(-5, -5)
test_modf_negative(-10, -10)
test_modf_negative(-100, -100)
test_modf_negative(-1000000, -1000000)

print("PASS: All negative integer modf tests passed with negative zero preservation")