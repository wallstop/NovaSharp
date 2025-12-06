-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:90
-- @test: ProcessorCoroutineCloseTUnitTests.CloseAfterExceptionReturnsFalseTuple
function blow()
                  error('boom!')
                end
