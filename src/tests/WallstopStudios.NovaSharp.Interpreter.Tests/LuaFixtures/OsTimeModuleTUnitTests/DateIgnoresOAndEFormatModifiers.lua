-- @lua-versions: 5.1, 5.2
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:204
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
-- @compat-notes: Lua 5.1/5.2: %O and %E modifiers are passed through as literals.
-- Lua 5.3+: throws "bad argument #1 to 'date' (invalid conversion specifier '%Ew')".
-- NovaSharp behavior: attempts to process O/E modifiers, may produce different results.
-- This test is NovaSharp-only since behavior varies significantly across Lua versions and platforms.

-- Test that NovaSharp handles O and E format modifiers gracefully
local result = os.date('!%OY-%Ew', 0)
if type(result) == "string" then
    print("PASS: result is " .. result)
else
    error("Expected string result from os.date")
end
