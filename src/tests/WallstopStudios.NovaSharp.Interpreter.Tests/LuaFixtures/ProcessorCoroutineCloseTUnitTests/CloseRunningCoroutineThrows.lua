-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:161
-- @test: ProcessorCoroutineCloseTUnitTests.CloseRunningCoroutineThrows
-- @compat-notes: Lua 5.3+: bitwise operators
function close_running()
                    local worker = coroutine.create(function()
                        local current = coroutine.running()
                        coroutine.close(current)
                    end)

                    return coroutine.resume(worker)
                end
