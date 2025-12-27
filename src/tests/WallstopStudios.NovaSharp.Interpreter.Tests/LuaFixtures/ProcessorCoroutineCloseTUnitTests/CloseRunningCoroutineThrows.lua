-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:201
-- @test: ProcessorCoroutineCloseTUnitTests.CloseRunningCoroutineThrows
-- @compat-notes: Test targets Lua 5.1
function close_running()
                    local worker = coroutine.create(function()
                        local current = coroutine.running()
                        coroutine.close(current)
                    end)

                    return coroutine.resume(worker)
                end
