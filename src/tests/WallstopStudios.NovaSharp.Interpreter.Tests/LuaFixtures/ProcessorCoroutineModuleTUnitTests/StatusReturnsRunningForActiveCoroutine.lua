-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:195
-- @test: ProcessorCoroutineModuleTUnitTests.StatusReturnsRunningForActiveCoroutine
-- @compat-notes: Test targets Lua 5.1
function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
