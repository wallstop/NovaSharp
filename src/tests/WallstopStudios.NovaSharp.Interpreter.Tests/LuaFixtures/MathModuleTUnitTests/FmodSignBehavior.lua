-- Tests that math.fmod sign matches dividend (fmod/truncate toward zero behavior)
-- Unlike IEEERemainder which rounds to nearest, fmod truncates toward zero

-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.FmodSignBehavior
-- @compat-notes: fmod behavior is consistent across all Lua versions (sign matches dividend)

-- Test: positive dividend, positive divisor -> positive result
local r1 = math.fmod(5.5, 2)
print("fmod(5.5, 2) =", r1)
assert(r1 > 0, "fmod(5.5, 2) should be positive")
assert(math.abs(r1 - 1.5) < 0.0001, "fmod(5.5, 2) should be 1.5")

-- Test: negative dividend, positive divisor -> negative result (sign matches dividend)
local r2 = math.fmod(-5.5, 2)
print("fmod(-5.5, 2) =", r2)
assert(r2 < 0, "fmod(-5.5, 2) should be negative (sign matches dividend)")
assert(math.abs(r2 - (-1.5)) < 0.0001, "fmod(-5.5, 2) should be -1.5")

-- Test: positive dividend, negative divisor -> positive result (sign matches dividend)
local r3 = math.fmod(5.5, -2)
print("fmod(5.5, -2) =", r3)
assert(r3 > 0, "fmod(5.5, -2) should be positive (sign matches dividend)")
assert(math.abs(r3 - 1.5) < 0.0001, "fmod(5.5, -2) should be 1.5")

-- Test: negative dividend, negative divisor -> negative result (sign matches dividend)
local r4 = math.fmod(-5.5, -2)
print("fmod(-5.5, -2) =", r4)
assert(r4 < 0, "fmod(-5.5, -2) should be negative (sign matches dividend)")
assert(math.abs(r4 - (-1.5)) < 0.0001, "fmod(-5.5, -2) should be -1.5")

-- Additional test cases with integers
local r5 = math.fmod(7, 3)
print("fmod(7, 3) =", r5)
assert(r5 == 1, "fmod(7, 3) should be 1")

local r6 = math.fmod(-7, 3)
print("fmod(-7, 3) =", r6)
assert(r6 == -1, "fmod(-7, 3) should be -1 (sign matches dividend)")

local r7 = math.fmod(7, -3)
print("fmod(7, -3) =", r7)
assert(r7 == 1, "fmod(7, -3) should be 1 (sign matches dividend)")

local r8 = math.fmod(-7, -3)
print("fmod(-7, -3) =", r8)
assert(r8 == -1, "fmod(-7, -3) should be -1 (sign matches dividend)")

print("All sign behavior tests passed")
