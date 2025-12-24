-- Comprehensive tests for math.fmod basic/normal cases

-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.FmodBasicCases
-- @compat-notes: Basic fmod behavior is consistent across all Lua versions

local function approxEqual(a, b, eps)
    eps = eps or 0.0000001
    return math.abs(a - b) < eps
end

-- Basic integer cases
local r1 = math.fmod(10, 3)
print("fmod(10, 3) =", r1)
assert(r1 == 1, "fmod(10, 3) should be 1")

local r2 = math.fmod(10, 5)
print("fmod(10, 5) =", r2)
assert(r2 == 0, "fmod(10, 5) should be 0 (evenly divisible)")

local r3 = math.fmod(10, 10)
print("fmod(10, 10) =", r3)
assert(r3 == 0, "fmod(10, 10) should be 0")

-- Float dividend, integer divisor
local r4 = math.fmod(5.5, 2)
print("fmod(5.5, 2) =", r4)
assert(approxEqual(r4, 1.5), "fmod(5.5, 2) should be 1.5")

local r5 = math.fmod(5.75, 2)
print("fmod(5.75, 2) =", r5)
assert(approxEqual(r5, 1.75), "fmod(5.75, 2) should be 1.75")

-- Integer dividend, float divisor
local r6 = math.fmod(10, 3.5)
print("fmod(10, 3.5) =", r6)
assert(approxEqual(r6, 3.0), "fmod(10, 3.5) should be 3.0")

-- Float dividend, float divisor
local r7 = math.fmod(5.5, 1.5)
print("fmod(5.5, 1.5) =", r7)
assert(approxEqual(r7, 1.0), "fmod(5.5, 1.5) should be 1.0")

-- Zero dividend (should return 0)
local r8 = math.fmod(0, 5)
print("fmod(0, 5) =", r8)
assert(r8 == 0, "fmod(0, 5) should be 0")

local r9 = math.fmod(0, -5)
print("fmod(0, -5) =", r9)
assert(r9 == 0, "fmod(0, -5) should be 0")

-- Dividend smaller than divisor (should return dividend)
local r10 = math.fmod(2, 5)
print("fmod(2, 5) =", r10)
assert(r10 == 2, "fmod(2, 5) should be 2 (dividend smaller than divisor)")

local r11 = math.fmod(0.5, 5)
print("fmod(0.5, 5) =", r11)
assert(approxEqual(r11, 0.5), "fmod(0.5, 5) should be 0.5")

-- Very small numbers
local r12 = math.fmod(0.001, 0.0003)
print("fmod(0.001, 0.0003) =", r12)
assert(approxEqual(r12, 0.0001), "fmod(0.001, 0.0003) should be approximately 0.0001")

-- Large numbers
local r13 = math.fmod(1000000, 7)
print("fmod(1000000, 7) =", r13)
assert(r13 == 1, "fmod(1000000, 7) should be 1")

print("All basic fmod tests passed")
