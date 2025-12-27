-- Tests that math.modf(+n) returns positive zero for the fractional part
-- when the input is a positive integer.
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

local function test_modf_positive(input, expected_int)
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

  -- Check fractional part is positive zero
  -- 1/(+0) = +inf, 1/(-0) = -inf
  local is_pos_zero = (frac_part == 0 and 1 / frac_part == math.huge)
  if not is_pos_zero then
    error(string.format("FAIL: math.modf(%d) fractional part should be +0 (positive zero), but 1/frac_part = %s",
      input, tostring(1 / frac_part)))
  end
end

-- Test cases for positive integers
test_modf_positive(1, 1)
test_modf_positive(5, 5)
test_modf_positive(10, 10)
test_modf_positive(100, 100)
test_modf_positive(1000000, 1000000)

print("PASS: All positive integer modf tests passed with positive zero preservation")