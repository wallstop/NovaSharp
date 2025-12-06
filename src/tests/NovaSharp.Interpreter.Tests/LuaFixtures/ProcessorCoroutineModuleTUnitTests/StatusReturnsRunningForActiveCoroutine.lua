-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:97
-- @test: ProcessorCoroutineModuleTUnitTests.StatusReturnsRunningForActiveCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
