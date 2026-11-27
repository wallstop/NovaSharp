-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:1825
-- @test: ProcessorCoroutineCloseTUnitTests.ClosingDeadCoroutineClearsSavedFailure
-- Compatibility notes: Test targets Lua 5.4+
local task = coroutine.create(function() error(nil) end)
                local resumed = coroutine.resume(task)
                local closedOnce, closeError = coroutine.close(task)
                local closedTwice, secondError = coroutine.close(task)

                return resumed, closedOnce, closeError, closedTwice, secondError
