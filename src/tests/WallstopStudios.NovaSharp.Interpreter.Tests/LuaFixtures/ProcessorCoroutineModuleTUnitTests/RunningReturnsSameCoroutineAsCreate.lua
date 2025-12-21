-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:678
-- @test: ProcessorCoroutineModuleTUnitTests.RunningReturnsSameCoroutineAsCreate
-- @compat-notes: Test targets Lua 5.1
function getRunning()
                    local co = coroutine.running()
                    return co
                end
