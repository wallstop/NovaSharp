-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:58
-- @test: DebugModuleTUnitTests.GetInfoReturnsNilWhenLevelExceedsStackDepth
return debug.getinfo(50)
