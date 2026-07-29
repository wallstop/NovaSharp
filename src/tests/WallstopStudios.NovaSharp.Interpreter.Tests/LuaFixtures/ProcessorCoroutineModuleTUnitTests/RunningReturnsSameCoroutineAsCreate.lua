-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:1070
-- @test: ProcessorCoroutineModuleTUnitTests.RunningReturnsSameCoroutineAsCreate
-- Compatibility notes: Test targets Lua 5.1
function getRunning()
                    local co = coroutine.running()
                    return co
                end
