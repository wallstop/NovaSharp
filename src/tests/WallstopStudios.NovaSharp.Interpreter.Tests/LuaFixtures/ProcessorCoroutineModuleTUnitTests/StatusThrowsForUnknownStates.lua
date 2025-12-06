-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:140
-- @test: ProcessorCoroutineModuleTUnitTests.StatusThrowsForUnknownStates
function idle()
                    return 1
                end
