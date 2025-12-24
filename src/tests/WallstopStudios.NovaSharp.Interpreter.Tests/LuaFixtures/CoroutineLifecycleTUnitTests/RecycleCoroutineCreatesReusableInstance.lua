-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:51
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineCreatesReusableInstance
-- @compat-notes: Test targets Lua 5.1
function first()
                    return 'done'
                end

                function second()
                    coroutine.yield('pause')
                    return 'done-again'
                end
