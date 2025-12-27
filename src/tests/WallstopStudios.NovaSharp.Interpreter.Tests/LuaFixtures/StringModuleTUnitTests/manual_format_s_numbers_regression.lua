-- Tests that string.format("%s", number) correctly converts numbers to strings.
-- This tests the fix for number-to-string coercion in the %s format specifier.
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

local function test_format(input, expected, desc)
  local result = string.format("%s", input)
  if result ~= expected then
    error(string.format("FAIL: string.format('%%s', %s) = '%s', expected '%s' (%s)",
      tostring(input), result, expected, desc))
  end
end

-- Test cases for various number types
test_format(123, "123", "positive integer")
test_format(-42, "-42", "negative integer")
test_format(0, "0", "zero")
test_format(123.456, "123.456", "positive float")
test_format(-123.456, "-123.456", "negative float")
test_format(0.5, "0.5", "fractional less than one")
test_format(-0.5, "-0.5", "negative fractional")

-- Test multiple numbers in one format string
local multi_result = string.format("Values: %s, %s, %s", 1, 2.5, -3)
if multi_result ~= "Values: 1, 2.5, -3" then
  error(string.format("FAIL: Multiple number format = '%s', expected 'Values: 1, 2.5, -3'", multi_result))
end

print("PASS: All string.format %s number tests passed")