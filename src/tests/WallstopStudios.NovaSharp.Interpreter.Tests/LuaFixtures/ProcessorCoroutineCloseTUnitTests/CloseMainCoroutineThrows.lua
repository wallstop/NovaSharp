-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:142
-- @test: ProcessorCoroutineCloseTUnitTests.CloseMainCoroutineThrows
-- @compat-notes: Lua 5.3+: bitwise operators
function close_main()
                    local current = select(1, coroutine.running())
                    coroutine.close(current)
                end
