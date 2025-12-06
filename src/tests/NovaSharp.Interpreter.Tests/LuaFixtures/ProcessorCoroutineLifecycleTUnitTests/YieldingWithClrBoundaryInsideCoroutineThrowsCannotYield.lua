-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineLifecycleTUnitTests.cs:90
-- @test: ProcessorCoroutineLifecycleTUnitTests.YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield
function boundary()
                    coroutine.yield('pause')
                    return 'done'
                end
