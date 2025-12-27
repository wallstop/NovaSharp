-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.MathTypeReturnsNilForNonNumeric
-- @compat-notes: math.type returns nil for non-numeric types (Lua 5.3+)

local function test(value, typeName)
    local result = math.type(value)
    assert(result == nil,
        string.format("math.type(%s) should return nil, got '%s'", typeName, tostring(result)))
    print(string.format("PASS: math.type(%s) = nil", typeName))
end

-- String
test("hello", "string")

-- Boolean
test(true, "boolean")

-- Nil
test(nil, "nil")

-- Table
test({}, "table")

print("All math.type non-numeric tests passed!")