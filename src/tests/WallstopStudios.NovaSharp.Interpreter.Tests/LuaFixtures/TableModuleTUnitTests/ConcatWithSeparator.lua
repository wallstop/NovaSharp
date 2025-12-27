-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.ConcatWithSeparator
-- @compat-notes: table.concat with various separators

local function test(sep, expected, desc)
    local t = { 'a', 'b', 'c', 'd' }
    local result = table.concat(t, sep)
    assert(result == expected,
        string.format("table.concat(t, '%s'): %s - expected '%s', got '%s'",
            sep, desc, expected, result))
    print(string.format("PASS: table.concat(t, '%s') = '%s' (%s)", sep, result, desc))
end

-- Comma separator
test(", ", "a, b, c, d", "comma separator")

-- Dash separator
test("-", "a-b-c-d", "dash separator")

-- Empty separator
test("", "abcd", "empty separator")

-- Multi-char separator
test("---", "a---b---c---d", "multi-char separator")

print("All table.concat separator tests passed!")