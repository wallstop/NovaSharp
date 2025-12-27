-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.FindPlainFlagSpecialCharacters
-- @compat-notes: string.find with plain=true treats pattern chars as literals

local function test(haystack, needle, expected_start, expected_end, desc)
    local start_idx, end_idx = string.find(haystack, needle, 1, true)
    assert(start_idx == expected_start,
        string.format("%s: start expected %d, got %s", desc, expected_start, tostring(start_idx)))
    assert(end_idx == expected_end,
        string.format("%s: end expected %d, got %s", desc, expected_end, tostring(end_idx)))
    print(string.format("PASS: string.find('%s', '%s', 1, true) = %d, %d (%s)",
        haystack, needle, start_idx, end_idx, desc))
end

-- Dot literal with plain flag
test("a.b", "a.b", 1, 3, "dot literal with plain flag")

-- Percent literal with plain flag
test("a%b", "a%b", 1, 3, "percent literal with plain flag")

-- Brackets literal with plain flag
test("[test]", "[test]", 1, 6, "brackets literal with plain flag")

-- Asterisk literal with plain flag
test("a*b", "a*b", 1, 3, "asterisk literal with plain flag")

-- Caret literal with plain flag
test("^start", "^start", 1, 6, "caret literal with plain flag")

-- Dollar literal with plain flag
test("end$", "end$", 1, 4, "dollar literal with plain flag")

print("All string.find plain flag tests passed!")