-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsTupleInLua52Plus
-- @compat-notes: Lua 5.2+ os.execute returns (true|nil, "exit"|"signal", code) tuple

-- Test: os.execute returns a tuple in Lua 5.2+
-- Reference: Lua 5.2+ Reference Manual §6.9

-- os.execute with "true" always succeeds
local a, b, c = os.execute("true")

local result_info = {
    first_type = type(a),
    first_value = a,
    second_type = type(b),
    second_value = b,
    third_type = type(c),
    third_value = c,
    is_success_tuple = (a == true) and (b == "exit") and (c == 0)
}

-- In Lua 5.2+, successful execution returns (true, "exit", 0)
print("First value: " .. tostring(result_info.first_value) .. " (" .. result_info.first_type .. ")")
print("Second value: " .. tostring(result_info.second_value) .. " (" .. result_info.second_type .. ")")
print("Third value: " .. tostring(result_info.third_value) .. " (" .. result_info.third_type .. ")")
print("Is success tuple: " .. tostring(result_info.is_success_tuple))

return result_info.is_success_tuple
