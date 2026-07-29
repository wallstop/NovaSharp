-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorDebuggerRuntimeTUnitTests.cs:26
-- @test: ProcessorDebuggerRuntimeTUnitTests.RefreshDebuggerThreadsUsesParentCoroutineStack
function idle() return 5 end
