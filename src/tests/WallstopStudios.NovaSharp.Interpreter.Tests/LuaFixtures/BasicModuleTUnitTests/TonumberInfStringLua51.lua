-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.TonumberInfStringLua51
-- @compat-notes: Lua 5.1 accepts infinity string literals via C's strtod; Lua 5.2+ rejects them (returns nil). NovaSharp currently does NOT support this - see commit 3be26a45ac9f4689d939156aec68b516cbec3222 for context.

-- Test that Lua 5.1 accepts various infinity string formats and returns a number

local function test_inf_string(input, expected_sign, label)
    local result = tonumber(input)
    if type(result) ~= "number" then
        error(string.format("FAIL: tonumber(%q) expected number, got %s", input, type(result)))
    end
    -- Check if it's actually infinity (finite check)
    local is_inf = (result == result) and (result + 1 == result) -- only true for infinity
    if not is_inf then
        error(string.format("FAIL: tonumber(%q) returned %s, expected infinity", input, tostring(result)))
    end
    -- Check sign
    if expected_sign == "positive" and result < 0 then
        error(string.format("FAIL: tonumber(%q) expected positive infinity, got %s", input, tostring(result)))
    elseif expected_sign == "negative" and result > 0 then
        error(string.format("FAIL: tonumber(%q) expected negative infinity, got %s", input, tostring(result)))
    end
    print(string.format("PASS: tonumber(%q) = %s (type: %s)", input, tostring(result), type(result)))
end

-- Basic inf variants (positive)
test_inf_string("inf", "positive", "lowercase inf")
test_inf_string("Inf", "positive", "title case Inf")
test_inf_string("INF", "positive", "uppercase INF")

-- Inf with sign prefix
test_inf_string("-inf", "negative", "negative lowercase inf")
test_inf_string("+inf", "positive", "positive lowercase inf")
test_inf_string("-Inf", "negative", "negative title case Inf")
test_inf_string("+Inf", "positive", "positive title case Inf")
test_inf_string("-INF", "negative", "negative uppercase INF")
test_inf_string("+INF", "positive", "positive uppercase INF")

-- Infinity spelled out
test_inf_string("infinity", "positive", "lowercase infinity")
test_inf_string("Infinity", "positive", "title case Infinity")
test_inf_string("INFINITY", "positive", "uppercase INFINITY")

-- Infinity with sign prefix
test_inf_string("-infinity", "negative", "negative lowercase infinity")
test_inf_string("+infinity", "positive", "positive lowercase infinity")
test_inf_string("-Infinity", "negative", "negative title case Infinity")
test_inf_string("+Infinity", "positive", "positive title case Infinity")
test_inf_string("-INFINITY", "negative", "negative uppercase INFINITY")
test_inf_string("+INFINITY", "positive", "positive uppercase INFINITY")

-- Inf with whitespace
test_inf_string(" inf", "positive", "inf with leading space")
test_inf_string("inf ", "positive", "inf with trailing space")
test_inf_string(" inf ", "positive", "inf with surrounding spaces")
test_inf_string("\tinf", "positive", "inf with leading tab")
test_inf_string("\ninf", "positive", "inf with leading newline")

print("All infinity string tests passed for Lua 5.1")
