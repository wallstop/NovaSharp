-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:80
-- @test: LoadModuleTUnitTests.RequireErrorListsSearchedPathsWhenLuaCompatibleErrorsEnabled
-- @compat-notes: Test targets Lua 5.1
return require('nonexistent_module')
