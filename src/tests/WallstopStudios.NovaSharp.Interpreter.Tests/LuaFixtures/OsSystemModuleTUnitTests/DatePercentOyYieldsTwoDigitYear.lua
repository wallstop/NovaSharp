-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:485
-- @test: OsSystemModuleTUnitTests.DatePercentOyOutputsLiteralTextInLua51
-- @compat-notes: Lua 5.1 outputs unknown format specifiers as literal text

-- Reference: lua5.1 -e "print(os.date('%Oy', 0))" outputs "%Oy"
local result = os.date('!%Oy', 0)
assert(result == "%Oy", "Expected literal '%Oy', got: " .. tostring(result))
return result