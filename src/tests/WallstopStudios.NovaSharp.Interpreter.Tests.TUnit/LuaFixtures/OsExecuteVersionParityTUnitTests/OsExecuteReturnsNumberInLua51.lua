-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNumberInLua51
-- @compat-notes: Lua 5.1 os.execute returns just exit status code as number

-- Test: os.execute returns a single number in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.8

-- os.execute with "true" always succeeds (returns 0)
local result = os.execute("true")

local result_info = {
    result_type = type(result),
    is_number = type(result) == "number"
}

-- In Lua 5.1, result should be a number (the exit code)
print("Result type: " .. result_info.result_type)
print("Is number: " .. tostring(result_info.is_number))

return result_info.is_number
