-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @test: MathModuleTUnitTests.ToIntegerReturnsNilForFunction
-- @compat-notes: Lua 5.3+: math.tointeger returns nil for function type (not an error)
-- Reference: Lua 5.3 Manual §6.7

local result = math.tointeger(function() end)
if result == nil then
    print("PASS")
else
    error("Expected nil, got: " .. tostring(result))
end
