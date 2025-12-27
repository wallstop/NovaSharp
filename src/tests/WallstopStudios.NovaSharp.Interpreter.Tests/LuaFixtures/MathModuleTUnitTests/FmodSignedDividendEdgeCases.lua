-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.FmodSignedDividendEdgeCases
-- @compat-notes: math.fmod result sign follows dividend sign

local function approx_eq(a, b, eps)
    eps = eps or 1e-12
    return math.abs(a - b) < eps
end

local function test(dividend, divisor, expected, desc)
    local result = math.fmod(dividend, divisor)
    assert(approx_eq(result, expected),
        string.format("math.fmod(%s, %s): %s - expected %s, got %s",
            tostring(dividend), tostring(divisor), desc, tostring(expected), tostring(result)))
    print(string.format("PASS: math.fmod(%s, %s) = %s (%s)",
        tostring(dividend), tostring(divisor), tostring(result), desc))
end

-- positive/positive
test(10, 3, 1, "positive/positive")

-- negative/positive - result is negative
test(-10, 3, -1, "negative/positive")

-- positive/negative - result is positive
test(10, -3, 1, "positive/negative")

-- negative/negative - result is negative
test(-10, -3, -1, "negative/negative")

-- float/float
test(5.5, 2.5, 0.5, "float/float")

-- negative float/positive float
test(-5.5, 2.5, -0.5, "negative float/positive float")

print("All math.fmod signed dividend edge case tests passed!")