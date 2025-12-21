-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorDebuggerRuntimeTUnitTests.cs:72
-- @test: ProcessorDebuggerRuntimeTUnitTests.RuntimeExceptionRefreshesDebuggerWhenSignalRequestsPause
local function explode()
                    error('boom')
                end
                explode()
