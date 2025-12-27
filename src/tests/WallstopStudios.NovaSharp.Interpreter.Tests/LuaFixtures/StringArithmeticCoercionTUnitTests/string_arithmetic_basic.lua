-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.StringArithmeticWorksWithNumberStrings
-- @compat-notes: String-to-number coercion works in all Lua versions for arithmetic

-- Test: String arithmetic with numeric strings works across all versions
-- Reference: Lua 5.x manual §3.4.3

-- Addition
assert("3" + "2" == 5, '"3" + "2" should equal 5')
assert("10" + 5 == 15, '"10" + 5 should equal 15')
assert(5 + "10" == 15, '5 + "10" should equal 15')

-- Subtraction
assert("10" - "3" == 7, '"10" - "3" should equal 7')

-- Multiplication
assert("3" * "4" == 12, '"3" * "4" should equal 12')

-- Division
assert("10" / "2" == 5, '"10" / "2" should equal 5')

-- Modulo
assert("7" % "3" == 1, '"7" % "3" should equal 1')

-- Power
assert("2" ^ "3" == 8, '"2" ^ "3" should equal 8')

-- Unary minus
assert(-"5" == -5, '-"5" should equal -5')

print("String arithmetic with numeric strings works")
return true
