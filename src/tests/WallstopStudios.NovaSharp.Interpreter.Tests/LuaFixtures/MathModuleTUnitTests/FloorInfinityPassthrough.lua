-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.FloorInfinityPassthrough
-- @compat-notes: Infinity values pass through math.floor unchanged

-- Test positive infinity
local pos_inf = math.floor(math.huge)
assert(pos_inf == math.huge, "math.floor(+inf) should return +inf")
print("PASS: math.floor(math.huge) = math.huge")

-- Test negative infinity
local neg_inf = math.floor(-math.huge)
assert(neg_inf == -math.huge, "math.floor(-inf) should return -inf")
print("PASS: math.floor(-math.huge) = -math.huge")

print("All math.floor infinity tests passed!")