-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoreLifecycleTUnitTests.cs:234
-- @test: ProcessorCoreLifecycleTUnitTests.YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield
function boundary()
                    coroutine.yield('pause')
                    return 'done'
                end
