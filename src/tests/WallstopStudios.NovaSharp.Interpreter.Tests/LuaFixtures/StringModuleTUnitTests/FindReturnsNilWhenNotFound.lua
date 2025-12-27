-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.FindReturnsNilWhenNotFound
-- @compat-notes: string.find returns nil when pattern not found

local function test(haystack, needle, desc)
    local result = string.find(haystack, needle)
    assert(result == nil,
        string.format("string.find('%s', '%s'): %s - expected nil, got %s",
            haystack, needle, desc, tostring(result)))
    print(string.format("PASS: string.find('%s', '%s') = nil (%s)", haystack, needle, desc))
end

-- Substring not present
test("hello", "xyz", "substring not present")

-- Case mismatch
test("hello", "HELLO", "case mismatch")

-- Empty haystack
test("", "a", "empty haystack")

-- Needle longer than haystack
test("hello", "hello world", "needle longer than haystack")

print("All string.find nil tests passed!")