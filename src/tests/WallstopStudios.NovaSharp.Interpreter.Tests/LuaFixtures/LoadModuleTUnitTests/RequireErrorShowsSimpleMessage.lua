-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.RequireErrorShowsSimpleMessageWhenLuaCompatibleErrorsDisabled
-- @compat-notes: Tests that require errors show simple message when LuaCompatibleErrors is disabled
-- @novasharp-options: LuaCompatibleErrors=false

-- This test verifies that when require() fails to find a module and
-- LuaCompatibleErrors is disabled (the default), the error message is
-- a simple "module 'X' not found" for backward compatibility.
--
-- This is a NovaSharp-only behavior - reference Lua always includes search paths.
--
-- Expected error: module 'nonexistent_module_for_testing' not found

return require('nonexistent_module_for_testing')
