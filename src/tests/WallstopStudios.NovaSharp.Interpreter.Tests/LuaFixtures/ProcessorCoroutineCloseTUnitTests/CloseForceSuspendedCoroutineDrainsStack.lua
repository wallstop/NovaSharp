-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:61
-- @test: ProcessorCoroutineCloseTUnitTests.CloseForceSuspendedCoroutineDrainsStack
-- @compat-notes: Lua 5.3+: bitwise operators
function slow()
                    for i = 1, 200 do end
                    return 'done'
                end
