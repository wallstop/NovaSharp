-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:229
-- @test: DebugModuleTapParityTUnitTests.SetUserValueRejectsNonTablesWithLuaMessage
local handle = io.stdout
local ok, valueOrError = pcall(function()
    return debug.setuservalue(handle, true)
end)
local storedValue = debug.getuservalue(handle)
if _VERSION == "Lua 5.2" then
    print(
        "setuservalue",
        ok,
        string.find(valueOrError, "table expected") ~= nil,
        storedValue == nil
    )
else
    print("setuservalue", ok)
end
