-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1028
-- @test: IoModuleTUnitTests.StdErrFlushReturnsTrue
-- @compat-notes: Test targets Lua 5.1
return io.stderr:flush()
