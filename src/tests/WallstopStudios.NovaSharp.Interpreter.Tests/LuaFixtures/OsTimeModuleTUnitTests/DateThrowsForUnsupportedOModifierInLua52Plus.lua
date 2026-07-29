-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:315
-- @test: OsTimeModuleTUnitTests.DateThrowsForUnsupportedOModifierInLua52Plus
-- Test targets Lua 5.1
return os.date('!%OY', 0)
