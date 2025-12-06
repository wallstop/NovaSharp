-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:39
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineCreatesReusableInstance
function first()
                    return 'done'
                end

                function second()
                    coroutine.yield('pause')
                    return 'done-again'
                end
