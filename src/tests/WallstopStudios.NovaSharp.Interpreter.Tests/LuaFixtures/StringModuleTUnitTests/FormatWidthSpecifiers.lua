-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.FormatWidthSpecifiersInteger, StringModuleTUnitTests.FormatWidthSpecifiersString
-- string.format width specifier tests

local function test(format, value, expected, desc)
    local result = string.format(format, value)
    assert(result == expected, 
        string.format("string.format('%s', %s): %s - expected '%s', got '%s'", 
            format, tostring(value), desc, expected, result))
    print(string.format("PASS: string.format('%s', %s) = '%s' (%s)", format, tostring(value), result, desc))
end

-- Right-padded integer
test("%5d", 42, "   42", "right-padded integer")

-- Left-padded integer
test("%-5d", 42, "42   ", "left-padded integer")

-- Zero-padded integer
test("%05d", 42, "00042", "zero-padded integer")

-- Right-padded string
test("%5s", "hi", "   hi", "right-padded string")

-- Left-padded string
test("%-5s", "hi", "hi   ", "left-padded string")

print("All string.format width specifier tests passed!")
