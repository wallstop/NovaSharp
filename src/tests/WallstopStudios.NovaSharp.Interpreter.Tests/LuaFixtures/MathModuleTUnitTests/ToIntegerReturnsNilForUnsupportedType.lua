-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:191
-- @test: MathModuleTUnitTests.ToIntegerReturnsNilForUnsupportedType
-- @compat-notes: Lua 5.3+: math.tointeger returns nil for non-number/non-string types (not an error)
-- Reference: Lua 5.3 Manual §6.7

-- Test that math.tointeger returns nil for unsupported types (boolean, table, function, etc.)
local result = math.tointeger(true)
if result == nil then
    print("PASS")
else
    error("Expected nil, got: " .. tostring(result))
end
