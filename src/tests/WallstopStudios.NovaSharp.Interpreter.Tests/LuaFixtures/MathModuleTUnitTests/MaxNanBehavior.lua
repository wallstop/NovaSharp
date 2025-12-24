-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:0
-- @test: MathModuleTUnitTests.MaxNanBehavior
-- @compat-notes: NaN comparison behavior in Lua - NaN comparisons are always false
-- The "current max" starts as the first argument and only updates if a later arg is greater
-- Since NaN comparisons are always false, NaN never "wins" against a real value,
-- but if NaN is the first arg, it becomes the initial max and is never replaced

local nan = 0/0
local function isnan(x)
    return x ~= x
end

-- Test: math.max(1, nan) - NaN as second arg, comparison 1 < nan is false
-- So max stays at 1
local result1 = math.max(1, nan)
assert(result1 == 1, "math.max(1, nan) should return 1, got " .. tostring(result1))

-- Test: math.max(nan, 1) - NaN as first arg becomes initial max
-- Comparison nan < 1 is false, so max stays at nan
local result2 = math.max(nan, 1)
assert(isnan(result2), "math.max(nan, 1) should return NaN, got " .. tostring(result2))

-- Test: math.max(nan, nan) - both NaN returns NaN
local result3 = math.max(nan, nan)
assert(isnan(result3), "math.max(nan, nan) should return NaN, got " .. tostring(result3))

-- Test: math.max with infinity and NaN
-- math.max(-inf, nan): -inf is initial max, nan doesn't beat it, returns -inf
local result4 = math.max(-math.huge, nan)
assert(result4 == -math.huge, "math.max(-inf, nan) should return -inf, got " .. tostring(result4))

-- math.max(nan, -inf): nan is initial max, -inf < nan is false, returns nan
local result5 = math.max(nan, -math.huge)
assert(isnan(result5), "math.max(nan, -inf) should return NaN, got " .. tostring(result5))

-- math.max(inf, nan): inf is initial max, nan doesn't beat it, returns inf
local result6 = math.max(math.huge, nan)
assert(result6 == math.huge, "math.max(inf, nan) should return inf, got " .. tostring(result6))

-- math.max(nan, inf): nan is initial max, inf > nan is false, returns nan
local result7 = math.max(nan, math.huge)
assert(isnan(result7), "math.max(nan, inf) should return NaN, got " .. tostring(result7))

-- All tests passed
print("MaxNanBehavior: all tests passed")
return true
