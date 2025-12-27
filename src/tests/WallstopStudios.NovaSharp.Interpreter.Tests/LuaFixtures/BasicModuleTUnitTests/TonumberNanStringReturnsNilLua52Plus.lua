-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.TonumberNanStringReturnsNilLua52Plus
-- @compat-notes: Lua 5.2+ rejects NaN string literals (returns nil); Lua 5.1 accepts them via C's strtod

-- Test that Lua 5.2+ rejects various NaN string formats (returns nil)

local function test_nan_string_returns_nil(input, label)
    local result = tonumber(input)
    if result ~= nil then
        error(string.format("FAIL: tonumber(%q) expected nil, got %s (%s)", input, tostring(result), type(result)))
    end
    print(string.format("PASS: tonumber(%q) = nil", input))
end

-- Basic NaN variants
test_nan_string_returns_nil("nan", "lowercase nan")
test_nan_string_returns_nil("NaN", "mixed case NaN")
test_nan_string_returns_nil("NAN", "uppercase NAN")
test_nan_string_returns_nil("Nan", "title case Nan")

-- NaN with sign prefix
test_nan_string_returns_nil("-nan", "negative lowercase nan")
test_nan_string_returns_nil("+nan", "positive lowercase nan")
test_nan_string_returns_nil("-NaN", "negative mixed case NaN")
test_nan_string_returns_nil("+NaN", "positive mixed case NaN")
test_nan_string_returns_nil("-NAN", "negative uppercase NAN")
test_nan_string_returns_nil("+NAN", "positive uppercase NAN")
test_nan_string_returns_nil("-Nan", "negative title case Nan")
test_nan_string_returns_nil("+Nan", "positive title case Nan")

-- NaN with whitespace
test_nan_string_returns_nil(" nan", "nan with leading space")
test_nan_string_returns_nil("nan ", "nan with trailing space")
test_nan_string_returns_nil(" nan ", "nan with surrounding spaces")
test_nan_string_returns_nil("\tnan", "nan with leading tab")
test_nan_string_returns_nil("\nnan", "nan with leading newline")

print("All NaN string rejection tests passed for Lua 5.2+")
