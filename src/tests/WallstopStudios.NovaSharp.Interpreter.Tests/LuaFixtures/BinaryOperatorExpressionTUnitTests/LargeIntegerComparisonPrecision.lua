-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\Expressions\BinaryOperatorExpressionTUnitTests.cs:LessThanPreservesIntegerPrecisionAtBoundaries
-- @test: BinaryOperatorExpressionTUnitTests.LessThanPreservesIntegerPrecisionAtBoundaries
-- @compat-notes: Verifies large integer comparisons preserve precision (Lua 5.3+ integers)

-- Test: Large integers near maxinteger should compare correctly
-- Expected: true (maxinteger - 1 < maxinteger)
local max = math.maxinteger
local maxMinusOne = max - 1
print(maxMinusOne < max)
return maxMinusOne < max
