-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineLifecycleTUnitTests.cs:90
-- @test: ProcessorCoroutineLifecycleTUnitTests.YieldingWithClrBoundaryInsideCoroutineThrowsCannotYield
function boundary()
                    coroutine.yield('pause')
                    return 'done'
                end
