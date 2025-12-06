-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoreLifecycleTUnitTests.cs:198
-- @test: ProcessorCoreLifecycleTUnitTests.CoroutineYieldPassesValuesWhenYieldingIsAllowed
function worker()
                    coroutine.yield('pause')
                    return 'done'
                end
