-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false

-- Test that Lua 5.1 accepts nan/inf string literals via tonumber()
-- This behavior comes from C's strtod() which parses these strings.
-- Lua 5.2+ changed this behavior to return nil for nan/inf strings.

local passed = true
local function check(description, result, expected_type, expected_check)
    local actual_type = type(result)
    if actual_type ~= expected_type then
        print("FAIL: " .. description .. " - expected type " .. expected_type .. ", got " .. actual_type)
        passed = false
        return
    end
    if expected_check and not expected_check(result) then
        print("FAIL: " .. description .. " - value check failed, got " .. tostring(result))
        passed = false
        return
    end
end

-- Helper to check if value is NaN (NaN ~= NaN is true)
local function isnan(x)
    return x ~= x
end

-- Helper to check if value is positive infinity
local function isposinf(x)
    return x == math.huge
end

-- Helper to check if value is negative infinity
local function isneginf(x)
    return x == -math.huge
end

-- Test NaN variations (case-insensitive)
check("tonumber('nan')", tonumber("nan"), "number", isnan)
check("tonumber('NaN')", tonumber("NaN"), "number", isnan)
check("tonumber('NAN')", tonumber("NAN"), "number", isnan)
check("tonumber('nAn')", tonumber("nAn"), "number", isnan)
check("tonumber('naN')", tonumber("naN"), "number", isnan)

-- Test NaN with whitespace (strtod accepts leading/trailing whitespace)
check("tonumber(' nan')", tonumber(" nan"), "number", isnan)
check("tonumber('nan ')", tonumber("nan "), "number", isnan)
check("tonumber(' nan ')", tonumber(" nan "), "number", isnan)
check("tonumber('  nan  ')", tonumber("  nan  "), "number", isnan)
check("tonumber('\\tnan')", tonumber("\tnan"), "number", isnan)
check("tonumber('nan\\t')", tonumber("nan\t"), "number", isnan)
check("tonumber('\\nnan')", tonumber("\nnan"), "number", isnan)

-- Test positive infinity variations
check("tonumber('inf')", tonumber("inf"), "number", isposinf)
check("tonumber('Inf')", tonumber("Inf"), "number", isposinf)
check("tonumber('INF')", tonumber("INF"), "number", isposinf)
check("tonumber('+inf')", tonumber("+inf"), "number", isposinf)
check("tonumber('+Inf')", tonumber("+Inf"), "number", isposinf)
check("tonumber('+INF')", tonumber("+INF"), "number", isposinf)

-- Test infinity (full word) variations
check("tonumber('infinity')", tonumber("infinity"), "number", isposinf)
check("tonumber('Infinity')", tonumber("Infinity"), "number", isposinf)
check("tonumber('INFINITY')", tonumber("INFINITY"), "number", isposinf)
check("tonumber('+infinity')", tonumber("+infinity"), "number", isposinf)
check("tonumber('+Infinity')", tonumber("+Infinity"), "number", isposinf)

-- Test negative infinity variations
check("tonumber('-inf')", tonumber("-inf"), "number", isneginf)
check("tonumber('-Inf')", tonumber("-Inf"), "number", isneginf)
check("tonumber('-INF')", tonumber("-INF"), "number", isneginf)
check("tonumber('-infinity')", tonumber("-infinity"), "number", isneginf)
check("tonumber('-Infinity')", tonumber("-Infinity"), "number", isneginf)
check("tonumber('-INFINITY')", tonumber("-INFINITY"), "number", isneginf)

-- Test infinity with whitespace
check("tonumber(' inf')", tonumber(" inf"), "number", isposinf)
check("tonumber('inf ')", tonumber("inf "), "number", isposinf)
check("tonumber(' inf ')", tonumber(" inf "), "number", isposinf)
check("tonumber(' -inf')", tonumber(" -inf"), "number", isneginf)
check("tonumber('-inf ')", tonumber("-inf "), "number", isneginf)
check("tonumber(' -inf ')", tonumber(" -inf "), "number", isneginf)
check("tonumber(' infinity ')", tonumber(" infinity "), "number", isposinf)
check("tonumber(' -infinity ')", tonumber(" -infinity "), "number", isneginf)

-- Verify the actual values are correct using math operations
local nan_val = tonumber("nan")
assert(nan_val ~= nan_val, "NaN should not equal itself")
assert(not (nan_val < 0), "NaN comparisons should return false")
assert(not (nan_val > 0), "NaN comparisons should return false")
assert(not (nan_val == 0), "NaN should not equal 0")

local inf_val = tonumber("inf")
assert(inf_val == math.huge, "inf should equal math.huge")
assert(inf_val > 0, "inf should be positive")
assert(inf_val + 1 == inf_val, "inf + 1 should still be inf")

local neginf_val = tonumber("-inf")
assert(neginf_val == -math.huge, "-inf should equal -math.huge")
assert(neginf_val < 0, "-inf should be negative")
assert(neginf_val - 1 == neginf_val, "-inf - 1 should still be -inf")

-- Verify arithmetic with nan/inf values
assert(isnan(nan_val + 1), "NaN + 1 should be NaN")
assert(isnan(nan_val * 2), "NaN * 2 should be NaN")
assert(isnan(nan_val / nan_val), "NaN / NaN should be NaN")
assert(isnan(inf_val - inf_val), "inf - inf should be NaN")
assert(isnan(inf_val / inf_val), "inf / inf should be NaN")
assert(isposinf(inf_val + inf_val), "inf + inf should be inf")
assert(isneginf(neginf_val + neginf_val), "-inf + -inf should be -inf")

if passed then
    print("PASS")
end
