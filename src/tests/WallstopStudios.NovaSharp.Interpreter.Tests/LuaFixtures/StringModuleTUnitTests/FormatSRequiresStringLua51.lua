-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:0
-- @test: StringModuleTUnitTests.FormatSRequiresStringLua51
-- @compat-notes: string.format %s errors on boolean/nil/table/function in Lua 5.1

-- In Lua 5.1, %s accepts strings and numbers (automatic coercion for numbers),
-- but errors on boolean, nil, table, and function types.
-- This behavior changed in Lua 5.2+ which uses tostring() for all types.

-- Test that %s errors on boolean (this should fail with "string expected, got boolean")
local result = string.format("%s", true)

-- If we reach here, the test failed (should have errored above)
return result
