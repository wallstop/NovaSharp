-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.SubEdgeCaseIndices
-- @compat-notes: string.sub edge cases with various indices

local function test(start_idx, end_idx, expected, desc)
    local result = string.sub('Hello', start_idx, end_idx)
    assert(result == expected,
        string.format("string.sub('Hello', %s, %s): %s - expected '%s', got '%s'",
            tostring(start_idx), tostring(end_idx), desc, expected, result))
    print(string.format("PASS: string.sub('Hello', %s, %s) = '%s' (%s)",
        tostring(start_idx), tostring(end_idx), result, desc))
end

-- Normal substring
test(1, 3, "Hel", "normal substring")

-- Zero start treated as 1
test(0, 3, "Hel", "zero start treated as 1")

-- Zero end before start returns empty
test(1, 0, "", "zero end before start returns empty")

-- Negative end means from end
test(1, -1, "Hello", "negative end means from end")

-- Negative start and end
test(-5, -1, "Hello", "negative start and end")

-- Partial negative range
test(-3, -1, "llo", "partial negative range")

-- End beyond length clamped
test(1, 100, "Hello", "end beyond length clamped")

-- Start before beginning clamped
test(-100, 3, "Hel", "start before beginning clamped")

-- Single character
test(3, 3, "l", "single character")

-- Start > end returns empty
test(5, 3, "", "start > end returns empty")

print("All string.sub edge case tests passed!")