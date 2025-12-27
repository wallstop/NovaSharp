-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSCoercesNonStringLua52Plus
-- @compat-notes: string.format %s coerces non-string values via tostring() in Lua 5.2+

-- In Lua 5.2+, %s automatically converts non-string values using tostring().
-- This is a change from Lua 5.1 which required string type for %s.

local results = {}

-- Test number coercion
results[#results + 1] = (string.format("%s", 123) == "123")

-- Test float coercion
local floatResult = string.format("%s", 123.456)
results[#results + 1] = (floatResult == "123.456")

-- Test boolean true coercion
results[#results + 1] = (string.format("%s", true) == "true")

-- Test boolean false coercion
results[#results + 1] = (string.format("%s", false) == "false")

-- Test nil coercion
results[#results + 1] = (string.format("%s", nil) == "nil")

-- Test string (should work in all versions)
results[#results + 1] = (string.format("%s", "hello") == "hello")

-- Verify all tests passed
for i, v in ipairs(results) do
    if not v then
        return false
    end
end

return true
