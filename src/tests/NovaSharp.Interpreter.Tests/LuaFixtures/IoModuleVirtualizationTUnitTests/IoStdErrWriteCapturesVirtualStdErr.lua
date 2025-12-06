-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:145
-- @test: IoModuleVirtualizationTUnitTests.IoStdErrWriteCapturesVirtualStdErr
io.stderr:write('failure'); io.stderr:flush();
