-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:363
-- @test: OsTimeModuleTUnitTests.DateSupportsEYModifierInLua52Plus
-- Test targets Lua 5.1
return os.date('!%EY', 0)
