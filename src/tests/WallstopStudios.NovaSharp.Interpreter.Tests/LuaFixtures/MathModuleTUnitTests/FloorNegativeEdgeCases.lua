-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.FloorNegativeEdgeCases
-- @compat-notes: math.floor edge cases with negative numbers

local function test(input, expected, desc)
    local result = math.floor(input)
    assert(result == expected, string.format("math.floor(%s): %s - expected %s, got %s", 
        tostring(input), desc, tostring(expected), tostring(result)))
    print(string.format("PASS: math.floor(%s) = %s (%s)", tostring(input), tostring(result), desc))
end

-- Negative float rounds toward negative infinity
test(-3.7, -4, "negative float rounds toward negative infinity")

-- Negative integer unchanged
test(-3.0, -3, "negative integer unchanged")

-- Small negative rounds to -1
test(-0.1, -1, "small negative rounds to -1")

-- Large negative fraction rounds to -1
test(-0.9, -1, "large negative fraction rounds to -1")

-- Zero unchanged
test(0.0, 0, "zero unchanged")

-- Negative zero unchanged (preserves sign in IEEE 754)
local neg_zero = -0.0
test(neg_zero, 0, "negative zero unchanged")

print("All math.floor negative edge case tests passed!")
