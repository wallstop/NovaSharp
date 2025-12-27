-- Tests math.modf with NaN returns (nan, nan) for both parts.
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

local nan = 0 / 0
local int_part, frac_part = math.modf(nan)

-- Check both parts are NaN (NaN is not equal to itself)
local int_is_nan = (int_part ~= int_part)
local frac_is_nan = (frac_part ~= frac_part)

if not int_is_nan then
    error(string.format("FAIL: math.modf(nan) integer part should be NaN, got %s", tostring(int_part)))
end

if not frac_is_nan then
    error(string.format("FAIL: math.modf(nan) fractional part should be NaN, got %s", tostring(frac_part)))
end

print("PASS: math.modf(NaN) returns (NaN, NaN)")