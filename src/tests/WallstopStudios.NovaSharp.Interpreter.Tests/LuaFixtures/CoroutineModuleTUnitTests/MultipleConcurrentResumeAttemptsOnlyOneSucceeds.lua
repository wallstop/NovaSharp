-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1127
-- @test: CoroutineModuleTUnitTests.MultipleConcurrentResumeAttemptsOnlyOneSucceeds
function pause()
                    waitForSignal()
                    return 'done'
                end
