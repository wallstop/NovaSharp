-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSErrorsOnNilLua51
-- @compat-notes: string.format %s errors on nil in Lua 5.1

-- In Lua 5.1, %s errors on nil type.
-- This behavior changed in Lua 5.2+ which uses tostring() for automatic coercion.

-- Test that %s errors on nil (this should fail with "string expected, got nil")
local result = string.format("%s", nil)

-- If we reach here, the test failed (should have errored above)
return result
