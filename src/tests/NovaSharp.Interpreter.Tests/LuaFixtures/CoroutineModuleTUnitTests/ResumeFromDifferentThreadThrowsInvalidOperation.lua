-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:863
-- @test: CoroutineModuleTUnitTests.ResumeFromDifferentThreadThrowsInvalidOperation
function pause()
                    waitForSignal()
                    return 'done'
                end
