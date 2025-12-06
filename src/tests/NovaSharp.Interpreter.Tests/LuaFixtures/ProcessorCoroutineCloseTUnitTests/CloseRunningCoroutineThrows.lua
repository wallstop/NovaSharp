-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:161
-- @test: ProcessorCoroutineCloseTUnitTests.CloseRunningCoroutineThrows
-- @compat-notes: Lua 5.3+: bitwise operators
function close_running()
                    local worker = coroutine.create(function()
                        local current = coroutine.running()
                        coroutine.close(current)
                    end)

                    return coroutine.resume(worker)
                end
