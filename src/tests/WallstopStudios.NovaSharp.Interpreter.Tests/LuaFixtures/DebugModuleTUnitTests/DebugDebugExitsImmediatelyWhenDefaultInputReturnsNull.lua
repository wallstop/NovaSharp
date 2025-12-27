-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:983
-- @test: DebugModuleTUnitTests.DebugDebugExitsImmediatelyWhenDefaultInputReturnsNull
-- @compat-notes: NovaSharp: debug.debug() is interactive/platform-dependent; Test targets Lua 5.1
return debug.debug()
