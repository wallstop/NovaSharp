-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:59
-- @test: DebugModuleTUnitTests.GetInfoReturnsNilWhenLevelExceedsStackDepth
return debug.getinfo(50)
