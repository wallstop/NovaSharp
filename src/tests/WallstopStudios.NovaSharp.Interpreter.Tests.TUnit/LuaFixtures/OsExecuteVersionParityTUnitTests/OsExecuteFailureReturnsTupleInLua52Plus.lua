-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteFailureReturnsTupleInLua52Plus
-- @compat-notes: Lua 5.2+ os.execute returns (nil, "exit"|"signal", code) on failure

-- Test: os.execute returns failure tuple in Lua 5.2+ when command fails
-- Reference: Lua 5.2+ Reference Manual §6.9

-- os.execute with "false" always fails (returns non-zero exit code)
local a, b, c = os.execute("false")

local result_info = {
    first_is_nil = a == nil,
    second_is_exit = b == "exit",
    third_is_number = type(c) == "number",
    third_value = c or -1,
    is_failure_tuple = (a == nil) and (b == "exit") and (type(c) == "number" and c ~= 0)
}

-- In Lua 5.2+, failed execution returns (nil, "exit", non-zero-code)
print("First is nil: " .. tostring(result_info.first_is_nil))
print("Second is 'exit': " .. tostring(result_info.second_is_exit))
print("Third is number: " .. tostring(result_info.third_is_number))
print("Third value: " .. tostring(result_info.third_value))
print("Is failure tuple: " .. tostring(result_info.is_failure_tuple))

return result_info.is_failure_tuple
