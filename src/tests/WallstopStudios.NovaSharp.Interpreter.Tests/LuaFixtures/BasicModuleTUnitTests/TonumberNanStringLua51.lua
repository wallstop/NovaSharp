-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.TonumberNanStringLua51
-- @compat-notes: Lua 5.1 accepts NaN string literals via C's strtod; Lua 5.2+ rejects them (returns nil)

-- Test that Lua 5.1 accepts various NaN string formats and returns a number

local function test_nan_string(input, label)
    local result = tonumber(input)
    if type(result) ~= "number" then
        error(string.format("FAIL: tonumber(%q) expected number, got %s", input, type(result)))
    end
    -- NaN has the special property that it's not equal to itself
    if result == result then
        error(string.format("FAIL: tonumber(%q) returned %s, expected NaN (not equal to itself)", input, tostring(result)))
    end
    print(string.format("PASS: tonumber(%q) = %s (type: %s)", input, tostring(result), type(result)))
end

-- Basic NaN variants
test_nan_string("nan", "lowercase nan")
test_nan_string("NaN", "mixed case NaN")
test_nan_string("NAN", "uppercase NAN")
test_nan_string("Nan", "title case Nan")

-- NaN with sign prefix
test_nan_string("-nan", "negative lowercase nan")
test_nan_string("+nan", "positive lowercase nan")
test_nan_string("-NaN", "negative mixed case NaN")
test_nan_string("+NaN", "positive mixed case NaN")
test_nan_string("-NAN", "negative uppercase NAN")
test_nan_string("+NAN", "positive uppercase NAN")
test_nan_string("-Nan", "negative title case Nan")
test_nan_string("+Nan", "positive title case Nan")

-- NaN with whitespace (strtod typically accepts leading/trailing whitespace)
test_nan_string(" nan", "nan with leading space")
test_nan_string("nan ", "nan with trailing space")
test_nan_string(" nan ", "nan with surrounding spaces")
test_nan_string("\tnan", "nan with leading tab")
test_nan_string("\nnan", "nan with leading newline")

print("All NaN string tests passed for Lua 5.1")
