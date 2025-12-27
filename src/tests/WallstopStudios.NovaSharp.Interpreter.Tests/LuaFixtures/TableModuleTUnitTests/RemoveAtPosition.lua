-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.RemoveAtPosition
-- @compat-notes: table.remove at specific position

local function test(pos, expected_removed, expected_remaining, desc)
    local t = { 'a', 'b', 'c', 'd' }
    local removed = table.remove(t, pos)
    local remaining = table.concat(t, '-')

    assert(removed == expected_removed,
        string.format("table.remove at pos %d: %s - expected removed '%s', got '%s'",
            pos, desc, expected_removed, tostring(removed)))
    assert(remaining == expected_remaining,
        string.format("table.remove at pos %d: %s - expected remaining '%s', got '%s'",
            pos, desc, expected_remaining, remaining))
    print(string.format("PASS: table.remove at pos %d returned '%s', remaining '%s' (%s)",
        pos, removed, remaining, desc))
end

-- Remove first
test(1, "a", "b-c-d", "remove first")

-- Remove second
test(2, "b", "a-c-d", "remove second")

-- Remove last
test(4, "d", "a-b-c", "remove last")

print("All table.remove position tests passed!")