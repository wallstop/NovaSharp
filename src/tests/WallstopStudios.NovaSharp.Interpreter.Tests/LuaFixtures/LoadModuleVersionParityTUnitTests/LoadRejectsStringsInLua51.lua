-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:116
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsStringsInLua51
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: load with string arg (5.2+)
load('return 1')
