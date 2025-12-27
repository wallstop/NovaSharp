-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.InsertAtPosition
-- @compat-notes: table.insert at specific position shifts elements

local function test(pos, value, expected, desc)
    local t = { 'a', 'b', 'c' }
    table.insert(t, pos, value)
    local result = table.concat(t, '-')
    assert(result == expected,
        string.format("table.insert at pos %d: %s - expected '%s', got '%s'",
            pos, desc, expected, result))
    print(string.format("PASS: table.insert at pos %d = '%s' (%s)", pos, result, desc))
end

-- Insert at beginning
test(1, "x", "x-a-b-c", "insert at beginning")

-- Insert at second position
test(2, "x", "a-x-b-c", "insert at second position")

-- Insert at third position
test(3, "x", "a-b-x-c", "insert at third position")

print("All table.insert position tests passed!")