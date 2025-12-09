-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:97
-- @test: ProcessorCoroutineModuleTUnitTests.StatusReturnsRunningForActiveCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
