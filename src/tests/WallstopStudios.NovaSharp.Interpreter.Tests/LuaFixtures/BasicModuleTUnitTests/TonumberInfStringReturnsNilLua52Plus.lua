-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.TonumberInfStringReturnsNilLua52Plus
-- @compat-notes: Lua 5.2+ rejects infinity string literals (returns nil); Lua 5.1 accepts them via C's strtod

-- Test that Lua 5.2+ rejects various infinity string formats (returns nil)

local function test_inf_string_returns_nil(input, label)
    local result = tonumber(input)
    if result ~= nil then
        error(string.format("FAIL: tonumber(%q) expected nil, got %s (%s)", input, tostring(result), type(result)))
    end
    print(string.format("PASS: tonumber(%q) = nil", input))
end

-- Basic inf variants
test_inf_string_returns_nil("inf", "lowercase inf")
test_inf_string_returns_nil("Inf", "title case Inf")
test_inf_string_returns_nil("INF", "uppercase INF")

-- Inf with sign prefix
test_inf_string_returns_nil("-inf", "negative lowercase inf")
test_inf_string_returns_nil("+inf", "positive lowercase inf")
test_inf_string_returns_nil("-Inf", "negative title case Inf")
test_inf_string_returns_nil("+Inf", "positive title case Inf")
test_inf_string_returns_nil("-INF", "negative uppercase INF")
test_inf_string_returns_nil("+INF", "positive uppercase INF")

-- Infinity spelled out
test_inf_string_returns_nil("infinity", "lowercase infinity")
test_inf_string_returns_nil("Infinity", "title case Infinity")
test_inf_string_returns_nil("INFINITY", "uppercase INFINITY")

-- Infinity with sign prefix
test_inf_string_returns_nil("-infinity", "negative lowercase infinity")
test_inf_string_returns_nil("+infinity", "positive lowercase infinity")
test_inf_string_returns_nil("-Infinity", "negative title case Infinity")
test_inf_string_returns_nil("+Infinity", "positive title case Infinity")
test_inf_string_returns_nil("-INFINITY", "negative uppercase INFINITY")
test_inf_string_returns_nil("+INFINITY", "positive uppercase INFINITY")

-- Inf with whitespace
test_inf_string_returns_nil(" inf", "inf with leading space")
test_inf_string_returns_nil("inf ", "inf with trailing space")
test_inf_string_returns_nil(" inf ", "inf with surrounding spaces")
test_inf_string_returns_nil("\tinf", "inf with leading tab")
test_inf_string_returns_nil("\ninf", "inf with leading newline")

-- Infinity with whitespace
test_inf_string_returns_nil(" infinity", "infinity with leading space")
test_inf_string_returns_nil("infinity ", "infinity with trailing space")
test_inf_string_returns_nil(" infinity ", "infinity with surrounding spaces")

print("All infinity string rejection tests passed for Lua 5.2+")
