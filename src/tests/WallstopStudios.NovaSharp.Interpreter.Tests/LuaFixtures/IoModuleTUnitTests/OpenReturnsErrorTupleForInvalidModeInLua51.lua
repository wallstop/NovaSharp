-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:99
-- @test: IoModuleTUnitTests.OpenReturnsErrorTupleForInvalidModeInLua51
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
return io.open('{path}', 'z')
