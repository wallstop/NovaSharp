-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineLifecycleTUnitTests.cs:56
-- @test: ProcessorCoroutineLifecycleTUnitTests.CoroutineYieldPassesValuesWhenYieldingIsAllowed
function worker()
                    coroutine.yield('pause')
                    return 'done'
                end
