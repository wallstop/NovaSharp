-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\Expressions\BinaryOperatorExpressionTUnitTests.cs:AdditionPreservesIntegerSubtype
-- @test: BinaryOperatorExpressionTUnitTests.AdditionPreservesIntegerSubtype
-- @compat-notes: Integer addition should preserve integer subtype in Lua 5.3+

-- Test: Integer + integer should produce integer
-- Expected: "integer" (not "float")
local a = 10  -- integer
local b = 20  -- integer
local result = a + b
print(math.type(result))
return math.type(result)
