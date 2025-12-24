-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test that Lua 5.2+ returns nil for nan/inf string literals
-- Starting from Lua 5.2, tonumber() no longer accepts "nan" and "inf" strings.
-- This was a deliberate change from Lua 5.1 behavior.

local passed = true
local function check_nil(description, result)
    if result ~= nil then
        print("FAIL: " .. description .. " - expected nil, got " .. type(result) .. ": " .. tostring(result))
        passed = false
    end
end

-- Test NaN variations (all should return nil)
check_nil("tonumber('nan')", tonumber("nan"))
check_nil("tonumber('NaN')", tonumber("NaN"))
check_nil("tonumber('NAN')", tonumber("NAN"))
check_nil("tonumber('nAn')", tonumber("nAn"))
check_nil("tonumber('naN')", tonumber("naN"))
check_nil("tonumber('Nan')", tonumber("Nan"))

-- Test NaN with whitespace (all should return nil)
check_nil("tonumber(' nan')", tonumber(" nan"))
check_nil("tonumber('nan ')", tonumber("nan "))
check_nil("tonumber(' nan ')", tonumber(" nan "))
check_nil("tonumber('  nan  ')", tonumber("  nan  "))
check_nil("tonumber('\\tnan')", tonumber("\tnan"))
check_nil("tonumber('nan\\t')", tonumber("nan\t"))
check_nil("tonumber('\\nnan')", tonumber("\nnan"))
check_nil("tonumber('nan\\n')", tonumber("nan\n"))

-- Test positive infinity variations (all should return nil)
check_nil("tonumber('inf')", tonumber("inf"))
check_nil("tonumber('Inf')", tonumber("Inf"))
check_nil("tonumber('INF')", tonumber("INF"))
check_nil("tonumber('iNf')", tonumber("iNf"))
check_nil("tonumber('+inf')", tonumber("+inf"))
check_nil("tonumber('+Inf')", tonumber("+Inf"))
check_nil("tonumber('+INF')", tonumber("+INF"))

-- Test infinity (full word) variations (all should return nil)
check_nil("tonumber('infinity')", tonumber("infinity"))
check_nil("tonumber('Infinity')", tonumber("Infinity"))
check_nil("tonumber('INFINITY')", tonumber("INFINITY"))
check_nil("tonumber('InFiNiTy')", tonumber("InFiNiTy"))
check_nil("tonumber('+infinity')", tonumber("+infinity"))
check_nil("tonumber('+Infinity')", tonumber("+Infinity"))
check_nil("tonumber('+INFINITY')", tonumber("+INFINITY"))

-- Test negative infinity variations (all should return nil)
check_nil("tonumber('-inf')", tonumber("-inf"))
check_nil("tonumber('-Inf')", tonumber("-Inf"))
check_nil("tonumber('-INF')", tonumber("-INF"))
check_nil("tonumber('-infinity')", tonumber("-infinity"))
check_nil("tonumber('-Infinity')", tonumber("-Infinity"))
check_nil("tonumber('-INFINITY')", tonumber("-INFINITY"))

-- Test infinity with whitespace (all should return nil)
check_nil("tonumber(' inf')", tonumber(" inf"))
check_nil("tonumber('inf ')", tonumber("inf "))
check_nil("tonumber(' inf ')", tonumber(" inf "))
check_nil("tonumber('  inf  ')", tonumber("  inf  "))
check_nil("tonumber(' -inf')", tonumber(" -inf"))
check_nil("tonumber('-inf ')", tonumber("-inf "))
check_nil("tonumber(' -inf ')", tonumber(" -inf "))
check_nil("tonumber(' +inf ')", tonumber(" +inf "))
check_nil("tonumber(' infinity ')", tonumber(" infinity "))
check_nil("tonumber(' -infinity ')", tonumber(" -infinity "))
check_nil("tonumber(' +infinity ')", tonumber(" +infinity "))
check_nil("tonumber('\\tinf')", tonumber("\tinf"))
check_nil("tonumber('inf\\t')", tonumber("inf\t"))
check_nil("tonumber('\\ninf')", tonumber("\ninf"))

-- Also verify with explicit base 10 (should still return nil)
check_nil("tonumber('nan', 10)", tonumber("nan", 10))
check_nil("tonumber('inf', 10)", tonumber("inf", 10))
check_nil("tonumber('-inf', 10)", tonumber("-inf", 10))
check_nil("tonumber('infinity', 10)", tonumber("infinity", 10))

-- Verify that normal number parsing still works
assert(tonumber("123") == 123, "Normal integer parsing should work")
assert(tonumber("123.456") == 123.456, "Normal float parsing should work")
assert(tonumber("-123") == -123, "Negative integer parsing should work")
assert(tonumber("+123") == 123, "Positive sign parsing should work")
assert(tonumber("1e10") == 1e10, "Scientific notation should work")
assert(tonumber("0x10") == 16, "Hex parsing should work")

-- Verify that we can still create NaN/Inf through math operations
local nan = 0/0
local inf = 1/0
local neginf = -1/0
assert(nan ~= nan, "NaN created via 0/0 should work")
assert(inf == math.huge, "Inf created via 1/0 should work")
assert(neginf == -math.huge, "-Inf created via -1/0 should work")

if passed then
    print("PASS")
end
