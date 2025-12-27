-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.ModfPositiveInfinity
-- @compat-notes: math.modf(inf) returns (inf, 0) - fractional part is 0, not NaN

local function is_negative_zero(x)
    return x == 0 and 1/x == -math.huge
end

local function is_positive_zero(x)
    return x == 0 and 1/x == math.huge
end

-- Test math.modf with positive infinity
local int_part, frac_part = math.modf(math.huge)

-- Integer part should be positive infinity
assert(int_part == math.huge, "integer part should be inf, got: " .. tostring(int_part))

-- Fractional part should be +0 (not NaN, and not -0)
assert(frac_part == 0, "fractional part should be 0, got: " .. tostring(frac_part))
assert(is_positive_zero(frac_part), "fractional part should be +0, not -0")

print("math.modf(math.huge) = " .. tostring(int_part) .. ", " .. tostring(frac_part))
print("PASS: math.modf positive infinity handling")
