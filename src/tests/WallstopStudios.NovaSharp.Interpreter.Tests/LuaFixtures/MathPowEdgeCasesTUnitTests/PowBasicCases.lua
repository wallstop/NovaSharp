-- Test: Basic math.pow/^ operations
-- Expected: Correct power results
-- Reference: Lua §6.7 (math.pow), §3.4.1 (arithmetic operators)

-- Basic integer exponents
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: standalone-fixture
-- @test: MathPowEdgeCasesTUnitTests.PowBasicCases
assert(math.pow(2, 3) == 8, "2^3 should equal 8")
assert(math.pow(10, 2) == 100, "10^2 should equal 100")
assert(math.pow(2, 10) == 1024, "2^10 should equal 1024")

-- Fractional exponents (roots)
assert(math.pow(4, 0.5) == 2, "4^0.5 should equal 2 (square root)")
assert(math.pow(8, 1/3) == 2, "8^(1/3) should equal 2 (cube root)")
assert(math.pow(16, 0.25) == 2, "16^0.25 should equal 2 (fourth root)")

-- Negative exponents
assert(math.pow(2, -1) == 0.5, "2^-1 should equal 0.5")
assert(math.pow(10, -2) == 0.01, "10^-2 should equal 0.01")
assert(math.pow(10, -3) == 0.001, "10^-3 should equal 0.001")

-- Negative base with integer exponents
assert(math.pow(-2, 2) == 4, "(-2)^2 should equal 4")
assert(math.pow(-2, 3) == -8, "(-2)^3 should equal -8")
assert(math.pow(-3, 2) == 9, "(-3)^2 should equal 9")
assert(math.pow(-3, 3) == -27, "(-3)^3 should equal -27")

-- Verify ^ operator matches math.pow
assert(2^3 == math.pow(2, 3), "2^3 should equal math.pow(2,3)")
assert(10^(-2) == math.pow(10, -2), "10^-2 should equal math.pow(10,-2)")
assert((-2)^3 == math.pow(-2, 3), "(-2)^3 should equal math.pow(-2,3)")

print("PASS: All basic pow tests passed")
