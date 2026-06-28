-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:221
-- @test: LoadModuleVersionParityTUnitTests.LoadsafeRejectsStringsInLua51
-- Test targets Lua 5.1
loadsafe('return 1')
