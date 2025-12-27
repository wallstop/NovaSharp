-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSErrorsOnFunctionLua51
-- @compat-notes: string.format %s errors on function in Lua 5.1

-- In Lua 5.1, %s errors on function type.
-- This behavior changed in Lua 5.2+ which uses tostring() for automatic coercion.

-- Test that %s errors on function (this should fail with "string expected, got function")
local result = string.format("%s", function() end)

-- If we reach here, the test failed (should have errored above)
return result
