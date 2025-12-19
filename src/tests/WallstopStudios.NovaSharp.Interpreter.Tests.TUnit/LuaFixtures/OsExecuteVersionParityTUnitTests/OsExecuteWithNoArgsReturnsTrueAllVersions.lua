-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteWithNoArgsReturnsTrueAllVersions
-- @compat-notes: os.execute() with no args returns true in all Lua versions to indicate shell availability

-- Test: os.execute() with no arguments returns true in all versions
-- Reference: Lua Reference Manual §6.9 (all versions)

local result = os.execute()

local result_info = {
    result_type = type(result),
    result_value = result,
    is_true = result == true
}

-- In all Lua versions, os.execute() with no args returns true (shell available)
print("Result type: " .. result_info.result_type)
print("Result value: " .. tostring(result_info.result_value))
print("Is true: " .. tostring(result_info.is_true))

return result_info.is_true
