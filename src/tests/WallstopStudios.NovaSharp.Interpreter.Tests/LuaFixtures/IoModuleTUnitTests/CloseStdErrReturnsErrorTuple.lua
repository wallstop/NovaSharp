-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:998
-- @test: IoModuleTUnitTests.CloseStdErrReturnsErrorTuple
-- @compat-notes: Test targets Lua 5.1
return io.close(io.stderr)
