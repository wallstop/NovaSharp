-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:176
-- @test: ProcessorCoroutineCloseTUnitTests.CloseMainCoroutineThrows
-- Test targets Lua 5.4+
function close_main()
                    local current = select(1, coroutine.running())
                    coroutine.close(current)
                end
