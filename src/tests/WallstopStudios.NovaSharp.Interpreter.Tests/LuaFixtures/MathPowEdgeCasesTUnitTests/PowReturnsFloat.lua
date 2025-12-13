-- @lua-versions: 5.3, 5.4
-- @source: MathPowEdgeCasesTUnitTests
-- @test: MathPowEdgeCasesTUnitTests.PowReturnsFloat
-- Test: Exponentiation always returns float subtype in Lua 5.3+
-- Expected: math.type returns "float" for all pow results
-- Reference: Lua §3.4.1 - "Exponentiation and float division always convert their operands to floats"

-- math.type is only available in Lua 5.3+
if not math.type then
  print("SKIP: math.type not available (requires Lua 5.3+)")
  return
end

-- Integer operands still produce float result
assert(math.type(2^3) == "float", "2^3 should return float type")
assert(math.type(math.pow(2, 3)) == "float", "math.pow(2,3) should return float type")

-- Float operands produce float result
assert(math.type(2.0^3) == "float", "2.0^3 should return float type")
assert(math.type(2^3.0) == "float", "2^3.0 should return float type")
assert(math.type(2.0^3.0) == "float", "2.0^3.0 should return float type")

-- Even when result is integer-like, type is still float
assert(math.type(4^0.5) == "float", "4^0.5 (=2.0) should return float type")
assert(math.type(2^0) == "float", "2^0 (=1.0) should return float type")

-- Edge cases also return float
assert(math.type(math.huge^0) == "float", "inf^0 should return float type")
assert(math.type(0^0) == "float", "0^0 should return float type")

print("PASS: All pow returns float tests passed")
