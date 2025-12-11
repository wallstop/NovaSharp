-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:720
-- @test: IoModuleTUnitTests.CloseStdErrReturnsErrorTuple
return io.close(io.stderr)
