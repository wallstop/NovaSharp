-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:61
-- @test: ProcessorCoroutineCloseTUnitTests.CloseForceSuspendedCoroutineDrainsStack
-- @compat-notes: Lua 5.3+: bitwise operators
function slow()
                    for i = 1, 200 do end
                    return 'done'
                end
