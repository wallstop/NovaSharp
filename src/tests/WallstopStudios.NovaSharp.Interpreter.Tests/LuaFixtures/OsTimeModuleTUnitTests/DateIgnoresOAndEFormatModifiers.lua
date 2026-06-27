-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs
-- @test: OsTimeModuleTUnitTests.DateOutputsUnsupportedOAndEAsLiteralTextInLua51
-- Platform-specific: macOS strftime interprets %O/%E modifiers differently than Linux. NovaSharp matches Linux/Ubuntu Lua behavior (outputs literal text for unsupported combinations)

-- Reference: lua5.1 -e "print(os.date('%OY-%Ew', 0))" outputs "%OY-%Ew"
-- %OY and %Ew are not valid POSIX combinations
local result = os.date('!%OY-%Ew', 0)
assert(result == "%OY-%Ew", "Expected literal '%OY-%Ew', got: " .. tostring(result))
return result