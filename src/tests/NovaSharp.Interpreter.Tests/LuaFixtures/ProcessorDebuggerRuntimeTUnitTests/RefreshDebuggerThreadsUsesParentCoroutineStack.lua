-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorDebuggerRuntimeTUnitTests.cs:22
-- @test: ProcessorDebuggerRuntimeTUnitTests.RefreshDebuggerThreadsUsesParentCoroutineStack
function idle() return 5 end
