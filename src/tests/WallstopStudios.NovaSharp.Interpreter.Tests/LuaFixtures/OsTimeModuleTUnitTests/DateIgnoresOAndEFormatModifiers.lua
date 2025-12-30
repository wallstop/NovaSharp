-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs
-- @test: OsTimeModuleTUnitTests.DateOutputsUnsupportedOAndEAsLiteralTextInLua51
-- @compat-notes: Lua 5.1 outputs unknown format specifiers as literal text

-- Reference: lua5.1 -e "print(os.date('%OY-%Ew', 0))" outputs "%OY-%Ew"
-- %OY and %Ew are not valid POSIX combinations
local result = os.date('!%OY-%Ew', 0)
assert(result == "%OY-%Ew", "Expected literal '%OY-%Ew', got: " .. tostring(result))
return result