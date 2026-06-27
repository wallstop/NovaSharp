-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:485
-- @test: OsSystemModuleTUnitTests.DatePercentOyOutputsLiteralTextInLua51
-- Platform-specific: macOS strftime interprets %O modifiers differently than Linux. NovaSharp matches Linux/Ubuntu Lua behavior (outputs literal text for unknown specifiers)

-- Reference: lua5.1 -e "print(os.date('%Oy', 0))" outputs "%Oy"
local result = os.date('!%Oy', 0)
assert(result == "%Oy", "Expected literal '%Oy', got: " .. tostring(result))
return result