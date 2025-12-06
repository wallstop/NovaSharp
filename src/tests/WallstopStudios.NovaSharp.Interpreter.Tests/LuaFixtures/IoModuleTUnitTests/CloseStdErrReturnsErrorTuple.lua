-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:700
-- @test: IoModuleTUnitTests.CloseStdErrReturnsErrorTuple
return io.close(io.stderr)
