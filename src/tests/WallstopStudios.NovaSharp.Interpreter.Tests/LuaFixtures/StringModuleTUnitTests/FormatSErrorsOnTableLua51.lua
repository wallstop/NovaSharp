-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSErrorsOnTableLua51
-- @compat-notes: string.format %s errors on table in Lua 5.1

-- In Lua 5.1, %s errors on table type.
-- This behavior changed in Lua 5.2+ which uses tostring() for automatic coercion.

-- Test that %s errors on table (this should fail with "string expected, got table")
local result = string.format("%s", {})

-- If we reach here, the test failed (should have errored above)
return result
