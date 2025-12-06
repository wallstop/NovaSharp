-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:720
-- @test: IoModuleTUnitTests.StdErrFlushReturnsTrue
return io.stderr:flush()
