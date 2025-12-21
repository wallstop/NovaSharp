-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1382
-- @test: DebugModuleTUnitTests.GetArgumentOrNilReturnsNilForOutOfBoundsIndex
-- @compat-notes: Test targets Lua 5.4+
return probe()
