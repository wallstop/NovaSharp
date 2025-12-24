-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSAcceptsNumbersLua51
-- @compat-notes: string.format %s accepts numbers in Lua 5.1 (auto-coercion)

-- In Lua 5.1, %s accepts strings AND numbers (automatic coercion for numbers).
-- However, it errors on boolean, nil, table, and function types.

local results = {}

-- Test integer coercion (works in Lua 5.1)
results[#results + 1] = (string.format("%s", 123) == "123")

-- Test float coercion (works in Lua 5.1)
local floatResult = string.format("%s", 123.456)
results[#results + 1] = (floatResult == "123.456")

-- Test negative number
results[#results + 1] = (string.format("%s", -42) == "-42")

-- Test zero
results[#results + 1] = (string.format("%s", 0) == "0")

-- Test string (should always work)
results[#results + 1] = (string.format("%s", "hello") == "hello")

-- Test empty string
results[#results + 1] = (string.format("%s", "") == "")

-- Verify all tests passed
for i, v in ipairs(results) do
    if not v then
        return false
    end
end

return true
