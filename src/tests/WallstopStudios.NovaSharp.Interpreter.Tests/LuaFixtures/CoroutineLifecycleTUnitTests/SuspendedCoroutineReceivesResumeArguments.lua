-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:196
-- @test: CoroutineLifecycleTUnitTests.SuspendedCoroutineReceivesResumeArguments
-- @compat-notes: Lua 5.3+: bitwise operators
function accumulator()
                    local first = coroutine.yield('ready')
                    return first * 2
                end
