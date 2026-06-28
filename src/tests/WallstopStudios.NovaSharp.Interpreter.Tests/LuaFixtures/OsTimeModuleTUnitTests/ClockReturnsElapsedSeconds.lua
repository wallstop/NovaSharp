-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:151
-- @test: OsTimeModuleTUnitTests.ClockReturnsElapsedSeconds
-- Test targets Lua 5.1
return os.clock()
