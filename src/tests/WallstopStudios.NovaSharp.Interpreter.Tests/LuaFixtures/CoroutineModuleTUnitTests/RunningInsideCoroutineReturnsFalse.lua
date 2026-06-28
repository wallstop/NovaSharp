-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:59
-- @test: CoroutineModuleTUnitTests.RunningInsideCoroutineReturnsFalse
-- Test targets Lua 5.2+
function runningCheck()
                    local _, isMain = coroutine.running()
                    return isMain
                end
