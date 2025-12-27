-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.CeilNegativeEdgeCases
-- @compat-notes: math.ceil edge cases with negative numbers

local function test(input, expected, desc)
  local result = math.ceil(input)
  assert(result == expected, string.format("math.ceil(%s): %s - expected %s, got %s",
    tostring(input), desc, tostring(expected), tostring(result)))
  print(string.format("PASS: math.ceil(%s) = %s (%s)", tostring(input), tostring(result), desc))
end

-- Negative float rounds toward zero
test(-3.7, -3, "negative float rounds toward zero")

-- Negative integer unchanged
test(-3.0, -3, "negative integer unchanged")

-- Small negative rounds to 0
test(-0.1, 0, "small negative rounds to 0")

-- Large negative fraction rounds to 0
test(-0.9, 0, "large negative fraction rounds to 0")

-- Zero unchanged
test(0.0, 0, "zero unchanged")

-- Small positive rounds to 1
test(0.1, 1, "small positive rounds to 1")

print("All math.ceil negative edge case tests passed!")