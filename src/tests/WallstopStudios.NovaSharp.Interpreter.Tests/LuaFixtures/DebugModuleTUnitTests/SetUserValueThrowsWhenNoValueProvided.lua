-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:585
-- @test: DebugModuleTUnitTests.SetUserValueThrowsWhenNoValueProvided
local handle = io.stdout
local seeded = {}
debug.setuservalue(handle, seeded)
local ok, valueOrError = pcall(function()
    return debug.setuservalue(handle)
end)
local storedValue = debug.getuservalue(handle)
if _VERSION == "Lua 5.2" then
    print("missing-value", ok, valueOrError == handle, storedValue == nil)
else
    print(
        "missing-value",
        ok,
        string.find(valueOrError, "value expected") ~= nil
    )
end
