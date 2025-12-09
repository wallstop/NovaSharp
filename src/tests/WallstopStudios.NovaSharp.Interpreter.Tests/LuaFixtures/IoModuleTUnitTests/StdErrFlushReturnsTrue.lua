-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:740
-- @test: IoModuleTUnitTests.StdErrFlushReturnsTrue
return io.stderr:flush()
