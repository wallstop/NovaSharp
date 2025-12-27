-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.MathTypeDistinguishesNumericTypes
-- @compat-notes: math.type was added in Lua 5.3

local function test(expr, value, expected, desc)
    local result = math.type(value)
    assert(result == expected,
        string.format("math.type(%s): %s - expected '%s', got '%s'",
            expr, desc, expected, tostring(result)))
    print(string.format("PASS: math.type(%s) = '%s' (%s)", expr, result, desc))
end

-- Integer literal
test("5", 5, "integer", "integer literal")

-- Float literal
test("3.14", 3.14, "float", "float literal")

-- Float with zero fraction
test("5.0", 5.0, "float", "float with zero fraction")

-- Result of math.floor
test("math.floor(3.5)", math.floor(3.5), "integer", "result of math.floor")

-- Division result is always float
test("1/2", 1 / 2, "float", "division result is always float")

-- Exponentiation result is float
test("2^10", 2 ^ 10, "float", "exponentiation result is float")

print("All math.type numeric types tests passed!")