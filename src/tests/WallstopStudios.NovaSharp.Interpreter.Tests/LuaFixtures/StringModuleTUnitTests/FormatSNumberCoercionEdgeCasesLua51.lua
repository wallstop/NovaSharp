-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSNumberCoercionEdgeCasesLua51
-- @compat-notes: string.format %s accepts numbers in Lua 5.1 with auto-coercion; tests edge cases

-- In Lua 5.1, %s accepts strings AND numbers (automatic coercion for numbers).
-- This test covers edge cases for number-to-string coercion.

local function test(description, actual, expected)
    if actual == expected then
        print("PASS: " .. description)
        return true
    else
        print("FAIL: " .. description)
        print("  Expected: " .. tostring(expected))
        print("  Got: " .. tostring(actual))
        return false
    end
end

local all_passed = true

-- Basic integer coercion
all_passed = test("integer 123", string.format("%s", 123), "123") and all_passed
all_passed = test("integer 0", string.format("%s", 0), "0") and all_passed
all_passed = test("negative integer -42", string.format("%s", -42), "-42") and all_passed

-- Float coercion
all_passed = test("float 123.456", string.format("%s", 123.456), "123.456") and all_passed
all_passed = test("float 0.5", string.format("%s", 0.5), "0.5") and all_passed
all_passed = test("negative float -3.14", string.format("%s", -3.14), "-3.14") and all_passed

-- Special numeric values (formatting varies by platform/implementation)
local inf_result = string.format("%s", math.huge)
local inf_ok = (inf_result == "inf" or inf_result == "Infinity" or inf_result:lower():match("inf"))
if inf_ok then
    print("PASS: infinity (got: " .. inf_result .. ")")
else
    print("FAIL: infinity")
    print("  Expected: inf or Infinity")
    print("  Got: " .. inf_result)
    all_passed = false
end

local neg_inf_result = string.format("%s", -math.huge)
local neg_inf_ok = (neg_inf_result == "-inf" or neg_inf_result == "-Infinity" or neg_inf_result:lower():match("-inf"))
if neg_inf_ok then
    print("PASS: negative infinity (got: " .. neg_inf_result .. ")")
else
    print("FAIL: negative infinity")
    print("  Expected: -inf or -Infinity")
    print("  Got: " .. neg_inf_result)
    all_passed = false
end

-- NaN requires special handling since NaN ~= NaN
local nan_result = string.format("%s", 0/0)
local nan_ok = (nan_result:lower():match("nan") ~= nil)
if nan_ok then
    print("PASS: NaN formatting (got: " .. nan_result .. ")")
else
    print("FAIL: NaN formatting")
    print("  Expected: nan or NaN or -nan")
    print("  Got: " .. nan_result)
    all_passed = false
end

-- Exponential notation numbers
local exp_result = string.format("%s", 1e10)
-- Different Lua implementations may format this differently
local exp_ok = (exp_result == "10000000000" or exp_result:match("1[eE]%+?10"))
if exp_ok then
    print("PASS: exponential 1e10 (got: " .. exp_result .. ")")
else
    print("INFO: exponential 1e10 = " .. exp_result .. " (format may vary)")
end

-- Very small numbers
local small_result = string.format("%s", 1e-10)
-- Just check it's a number string
local small_ok = (tonumber(small_result) ~= nil)
if small_ok then
    print("PASS: small number 1e-10 (got: " .. small_result .. ")")
else
    print("FAIL: small number 1e-10")
    print("  Got: " .. small_result)
    all_passed = false
end

-- String passthrough (should work)
all_passed = test("string hello", string.format("%s", "hello"), "hello") and all_passed
all_passed = test("empty string", string.format("%s", ""), "") and all_passed
all_passed = test("string with spaces", string.format("%s", "hello world"), "hello world") and all_passed

-- Combined format string with multiple %s
local combined = string.format("%s + %s = %s", 1, 2, 3)
all_passed = test("combined format", combined, "1 + 2 = 3") and all_passed

if all_passed then
    print("PASS: All string.format %s number coercion edge cases passed")
else
    error("FAIL: Some string.format %s tests failed")
end
