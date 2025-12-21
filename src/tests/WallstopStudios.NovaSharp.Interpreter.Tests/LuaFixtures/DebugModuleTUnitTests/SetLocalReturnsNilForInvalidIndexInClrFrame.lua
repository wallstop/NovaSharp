-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1042
-- @test: DebugModuleTUnitTests.SetLocalReturnsNilForInvalidIndexInClrFrame
-- @compat-notes: Test targets Lua 5.1
return probe()
