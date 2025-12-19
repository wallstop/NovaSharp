-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.LoadRejectsStringArgumentInLua51

-- Test: load() throws error when given string in Lua 5.1
-- Reference: Lua 5.1 manual - load only accepts functions
-- @compat-notes: Lua 5.1 load() only accepts functions; use loadstring() for strings

return load('function(')
