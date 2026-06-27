-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineModuleTUnitTests.cs:122
-- @test: ProcessorCoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse
-- Test targets Lua 5.2+
function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
