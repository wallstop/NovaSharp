-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.FloorDivisionWorksWithStrings
-- @compat-notes: Floor division (//) requires Lua 5.3+

-- Test: Floor division with strings works
-- Reference: Lua 5.3+ manual §3.4.1

assert("7" // "2" == 3, '"7" // "2" should equal 3')
assert("10" // "3" == 3, '"10" // "3" should equal 3')
assert(10 // "3" == 3, '10 // "3" should equal 3')
assert("10" // 3 == 3, '"10" // 3 should equal 3')

print("Floor division with strings works")
return true
