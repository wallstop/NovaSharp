-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:32
-- @test: ProcessorCoroutineCloseTUnitTests.CloseWhileSuspendedEndsCoroutine
function pause()
                  coroutine.yield('pause')
                  return 'done'
                end
