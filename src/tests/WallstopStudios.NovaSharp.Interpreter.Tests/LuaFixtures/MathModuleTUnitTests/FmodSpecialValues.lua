-- Tests math.fmod with special values: infinity, negative infinity, NaN

-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.FmodSpecialValues
-- @compat-notes: Behavior with special float values is consistent across Lua versions

local inf = math.huge
local ninf = -math.huge
local nan = 0/0

local function isNaN(x)
    return x ~= x
end

local function isInf(x)
    return x == inf or x == ninf
end

-- Test: infinity dividend, finite divisor -> NaN
local r1 = math.fmod(inf, 1)
print("fmod(inf, 1) =", r1, "isNaN:", isNaN(r1))
assert(isNaN(r1), "fmod(inf, 1) should be NaN")

local r2 = math.fmod(ninf, 1)
print("fmod(-inf, 1) =", r2, "isNaN:", isNaN(r2))
assert(isNaN(r2), "fmod(-inf, 1) should be NaN")

-- Test: finite dividend, infinity divisor -> dividend
local r3 = math.fmod(5, inf)
print("fmod(5, inf) =", r3)
assert(r3 == 5, "fmod(5, inf) should be 5 (dividend returned)")

local r4 = math.fmod(-5, inf)
print("fmod(-5, inf) =", r4)
assert(r4 == -5, "fmod(-5, inf) should be -5 (dividend returned)")

local r5 = math.fmod(5, ninf)
print("fmod(5, -inf) =", r5)
assert(r5 == 5, "fmod(5, -inf) should be 5 (dividend returned)")

local r6 = math.fmod(-5, ninf)
print("fmod(-5, -inf) =", r6)
assert(r6 == -5, "fmod(-5, -inf) should be -5 (dividend returned)")

-- Test: NaN dividend -> NaN
local r7 = math.fmod(nan, 1)
print("fmod(NaN, 1) =", r7, "isNaN:", isNaN(r7))
assert(isNaN(r7), "fmod(NaN, 1) should be NaN")

local r8 = math.fmod(nan, inf)
print("fmod(NaN, inf) =", r8, "isNaN:", isNaN(r8))
assert(isNaN(r8), "fmod(NaN, inf) should be NaN")

-- Test: finite dividend, NaN divisor -> NaN
local r9 = math.fmod(5, nan)
print("fmod(5, NaN) =", r9, "isNaN:", isNaN(r9))
assert(isNaN(r9), "fmod(5, NaN) should be NaN")

-- Test: inf / inf -> NaN
local r10 = math.fmod(inf, inf)
print("fmod(inf, inf) =", r10, "isNaN:", isNaN(r10))
assert(isNaN(r10), "fmod(inf, inf) should be NaN")

-- Test: zero dividend with infinity divisor -> 0
local r11 = math.fmod(0, inf)
print("fmod(0, inf) =", r11)
assert(r11 == 0, "fmod(0, inf) should be 0")

local r12 = math.fmod(0, ninf)
print("fmod(0, -inf) =", r12)
assert(r12 == 0, "fmod(0, -inf) should be 0")

print("All special values tests passed")
