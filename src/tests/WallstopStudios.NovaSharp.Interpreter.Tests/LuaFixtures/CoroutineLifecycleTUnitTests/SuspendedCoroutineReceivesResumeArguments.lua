-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\CoroutineLifecycleIntegrationTUnitTests.cs:196
-- @test: CoroutineLifecycleTUnitTests.SuspendedCoroutineReceivesResumeArguments
-- @compat-notes: Lua 5.3+: bitwise operators
function accumulator()
                    local first = coroutine.yield('ready')
                    return first * 2
                end
