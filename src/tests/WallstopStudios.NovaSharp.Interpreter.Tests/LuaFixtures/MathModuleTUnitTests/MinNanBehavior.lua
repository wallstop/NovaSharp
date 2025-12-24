-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.MinNanBehavior
-- @compat-notes: NaN comparison behavior in Lua - NaN comparisons are always false
-- The "current min" starts as the first argument and only updates if a later arg is smaller
-- Since NaN comparisons are always false, NaN never "wins" against a real value,
-- but if NaN is the first arg, it becomes the initial min and is never replaced

local nan = 0/0
local function isnan(x)
    return x ~= x
end

-- Test: math.min(1, nan) - NaN as second arg, comparison 1 > nan is false
-- So min stays at 1
local result1 = math.min(1, nan)
assert(result1 == 1, "math.min(1, nan) should return 1, got " .. tostring(result1))

-- Test: math.min(nan, 1) - NaN as first arg becomes initial min
-- Comparison nan > 1 is false, so min stays at nan
local result2 = math.min(nan, 1)
assert(isnan(result2), "math.min(nan, 1) should return NaN, got " .. tostring(result2))

-- Test: math.min(nan, nan) - both NaN returns NaN
local result3 = math.min(nan, nan)
assert(isnan(result3), "math.min(nan, nan) should return NaN, got " .. tostring(result3))

-- Test: math.min with infinity and NaN
-- math.min(inf, nan): inf is initial min, nan doesn't beat it, returns inf
local result4 = math.min(math.huge, nan)
assert(result4 == math.huge, "math.min(inf, nan) should return inf, got " .. tostring(result4))

-- math.min(nan, inf): nan is initial min, inf > nan is false, returns nan
local result5 = math.min(nan, math.huge)
assert(isnan(result5), "math.min(nan, inf) should return NaN, got " .. tostring(result5))

-- math.min(-inf, nan): -inf is initial min, nan doesn't beat it, returns -inf
local result6 = math.min(-math.huge, nan)
assert(result6 == -math.huge, "math.min(-inf, nan) should return -inf, got " .. tostring(result6))

-- math.min(nan, -inf): nan is initial min, -inf < nan is false, returns nan
local result7 = math.min(nan, -math.huge)
assert(isnan(result7), "math.min(nan, -inf) should return NaN, got " .. tostring(result7))

-- All tests passed
print("MinNanBehavior: all tests passed")
return true
