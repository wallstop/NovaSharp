-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:163
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead
-- @compat-notes: Test targets Lua 5.1
function busy()
                    for i = 1, 200 do end
                    return 'finished'
                end
