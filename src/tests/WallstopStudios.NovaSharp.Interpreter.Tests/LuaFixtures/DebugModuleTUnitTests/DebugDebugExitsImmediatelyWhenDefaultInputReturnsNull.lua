-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:503
-- @test: DebugModuleTUnitTests.DebugDebugExitsImmediatelyWhenDefaultInputReturnsNull
-- @compat-notes: NovaSharp: debug.debug() is interactive/platform-dependent
return debug.debug()
