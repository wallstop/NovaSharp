-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoreLifecycleTUnitTests.cs:201
-- @test: ProcessorCoreLifecycleTUnitTests.CoroutineYieldPassesValuesWhenYieldingIsAllowed
function worker()
                    coroutine.yield('pause')
                    return 'done'
                end
