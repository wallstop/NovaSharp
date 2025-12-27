-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\Expressions\BinaryOperatorExpressionTUnitTests.cs:FloorDivisionPreservesIntegerSubtype
-- @test: BinaryOperatorExpressionTUnitTests.FloorDivisionPreservesIntegerSubtype
-- @compat-notes: Integer floor division should preserve integer subtype in Lua 5.3+

-- Test: Integer // integer should produce integer
-- Expected: "integer" (not "float")
local a = 17
local b = 5
local result = a // b  -- 17 // 5 = 3
print(result)
print(math.type(result))
return result, math.type(result)
