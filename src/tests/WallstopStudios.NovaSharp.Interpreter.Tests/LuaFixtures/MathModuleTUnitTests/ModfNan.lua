-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfNan
-- @compat-notes: math.modf(NaN) returns (NaN, NaN) - both parts are NaN

local function is_nan(x)
    return x ~= x
end

-- Create NaN
local nan = 0/0

-- Test math.modf with NaN
local int_part, frac_part = math.modf(nan)

-- Both parts should be NaN
assert(is_nan(int_part), "integer part should be NaN, got: " .. tostring(int_part))
assert(is_nan(frac_part), "fractional part should be NaN, got: " .. tostring(frac_part))

print("math.modf(0/0) = " .. tostring(int_part) .. ", " .. tostring(frac_part))
print("int_part is NaN: " .. tostring(is_nan(int_part)))
print("frac_part is NaN: " .. tostring(is_nan(frac_part)))
print("PASS: math.modf NaN handling")
