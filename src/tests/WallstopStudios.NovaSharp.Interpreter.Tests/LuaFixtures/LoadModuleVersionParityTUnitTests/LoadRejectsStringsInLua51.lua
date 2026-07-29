-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:127
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsStringsInLua51
-- Test targets Lua 5.1; Lua 5.2+: load with string arg (5.2+)
load('return 1')
