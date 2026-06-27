-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.RequireErrorListsSearchedPathsWhenLuaCompatibleErrorsEnabled
-- Tests that require errors list all searched paths per Lua spec
-- @novasharp-options: LuaCompatibleErrors=true

-- This test verifies that when require() fails to find a module and
-- LuaCompatibleErrors is enabled, the error message includes all the
-- paths that were searched, matching reference Lua behavior.
--
-- Reference Lua error format:
--   module 'foo' not found:
--       no field package.preload['foo']
--       no file './foo.lua'
--       no file '/usr/local/share/lua/5.4/foo.lua'
--       ...

return require('nonexistent_module_for_testing')
