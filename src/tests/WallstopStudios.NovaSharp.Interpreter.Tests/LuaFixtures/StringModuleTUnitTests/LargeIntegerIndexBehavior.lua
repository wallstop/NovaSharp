-- Tests for large integer index handling at double precision boundaries
-- This fixture validates that integer vs float type distinctions are handled correctly

-- 2^53 is the maximum integer exactly representable as a double
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.LargeIntegerIndexBehavior
local limit = 9007199254740992  -- 2^53

-- 2^53 + 1 cannot be exactly represented as a double
local beyond_limit = 9007199254740993  -- 2^53 + 1

-- Test 1: Large integer stored as integer type should work
local result1 = string.byte("a", beyond_limit)
assert(result1 == nil, "Large integer index (2^53+1) should return nil for out-of-range")

-- Test 2: math.maxinteger should work (stored as integer)
local result2 = string.byte("a", math.maxinteger)
assert(result2 == nil, "math.maxinteger index should return nil for out-of-range")

-- Test 3: Whole number floats should work
local result3 = string.byte("hello", 5.0)
assert(result3 == 111, "Float 5.0 should work as index (returns 'o' = 111)")

-- Test 4: math.type shows the distinction between integer and float
local int_val = 9007199254740993
local float_val = 9007199254740993.0
assert(math.type(int_val) == "integer", "Literal integer should be integer type")
assert(math.type(float_val) == "float", "Literal with .0 should be float type")

-- Test 5: The float version gets rounded to 2^53 due to precision loss
-- (This is a property of IEEE 754, not Lua behavior)
assert(float_val == 9007199254740992.0, "Float 2^53+1 should round to 2^53")

-- Test 6: Both integer and rounded float should work as indices
local result5 = string.byte("a", int_val)
local result6 = string.byte("a", float_val)
assert(result5 == nil, "Integer 2^53+1 index should return nil")
assert(result6 == nil, "Float (rounded to 2^53) index should return nil")

print("All large integer index tests passed!")
