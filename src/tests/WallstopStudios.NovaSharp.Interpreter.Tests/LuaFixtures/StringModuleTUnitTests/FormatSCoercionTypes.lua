-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSCoercionTypes
-- @compat-notes: string.format %s coerces all types via tostring() in Lua 5.2+

-- In Lua 5.2+, %s uses tostring() to convert any value to string.
-- This test verifies the coercion behavior for all Lua types.

local results = {}

-- Integer coercion
results[#results + 1] = (string.format("%s", 123) == "123")

-- Float coercion  
local floatStr = string.format("%s", 123.456)
results[#results + 1] = (floatStr == "123.456")

-- Boolean true
results[#results + 1] = (string.format("%s", true) == "true")

-- Boolean false
results[#results + 1] = (string.format("%s", false) == "false")

-- Nil
results[#results + 1] = (string.format("%s", nil) == "nil")

-- Table - result starts with "table:"
local tableStr = string.format("%s", {})
results[#results + 1] = (string.sub(tableStr, 1, 6) == "table:")

-- Function - result starts with "function:"
local funcStr = string.format("%s", function() end)
results[#results + 1] = (string.sub(funcStr, 1, 9) == "function:")

-- String (should work unchanged)
results[#results + 1] = (string.format("%s", "hello") == "hello")

-- Empty string
results[#results + 1] = (string.format("%s", "") == "")

-- String with spaces
results[#results + 1] = (string.format("%s", "hello world") == "hello world")

-- Negative number
results[#results + 1] = (string.format("%s", -42) == "-42")

-- Zero
results[#results + 1] = (string.format("%s", 0) == "0")

-- Verify all tests passed
for i, v in ipairs(results) do
    if not v then
        return false
    end
end

return true
