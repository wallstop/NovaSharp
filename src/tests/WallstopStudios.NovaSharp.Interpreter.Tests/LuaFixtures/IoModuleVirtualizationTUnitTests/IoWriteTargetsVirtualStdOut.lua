-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:132
-- @test: IoModuleVirtualizationTUnitTests.IoWriteTargetsVirtualStdOut
io.write('first'); io.write('second'); io.flush();
