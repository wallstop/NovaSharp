-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringArithmeticCoercionTUnitTests.cs
-- @test: StringArithmeticCoercionTUnitTests.NonNumericStringArithmeticErrors
-- Non-numeric strings cause errors in arithmetic in all versions

-- Test: Non-numeric strings in arithmetic should error
-- Reference: Lua 5.x manual §3.4.3

local result = "hello" + 5  -- Should error: "hello" is not a number
