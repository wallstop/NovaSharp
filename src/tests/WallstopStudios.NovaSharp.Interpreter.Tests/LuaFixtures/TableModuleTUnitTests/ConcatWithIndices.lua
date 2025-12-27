-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.ConcatWithIndices
-- @compat-notes: table.concat with explicit indices

local function test(start_idx, end_idx, expected, desc)
    local t = { 'a', 'b', 'c', 'd' }
    local result = table.concat(t, '-', start_idx, end_idx)
    assert(result == expected,
        string.format("table.concat(t, '-', %d, %d): %s - expected '%s', got '%s'",
            start_idx, end_idx, desc, expected, result))
    print(string.format("PASS: table.concat(t, '-', %d, %d) = '%s' (%s)",
        start_idx, end_idx, result, desc))
end

-- Middle range
test(2, 3, "b-c", "middle range")

-- Full range
test(1, 4, "a-b-c-d", "full range")

-- Single element
test(1, 1, "a", "single element")

-- Single middle element
test(3, 3, "c", "single middle element")

-- Last element only
test(4, 4, "d", "last element only")

print("All table.concat indices tests passed!")