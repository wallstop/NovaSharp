-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.FormatQEscaping
-- @compat-notes: string.format %q produces valid Lua string literals

local function test(input, expected, desc)
    local result = string.format('%q', input)
    assert(result == expected, 
        string.format("string.format('%%q', '%s'): %s - expected %s, got %s", 
            input, desc, expected, result))
    print(string.format("PASS: string.format('%%q', '%s') = %s (%s)", input, result, desc))
end

-- Simple string
test("hello", '"hello"', "simple string")

-- Another simple string
test("world", '"world"', "another simple string")

-- Empty string
test("", '""', "empty string")

-- Alphanumeric string
test("test123", '"test123"', "alphanumeric string")

print("All string.format %q tests passed!")
