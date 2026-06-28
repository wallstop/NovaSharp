-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:1468
-- @test: CoroutineModuleTUnitTests.RunningReturnsIsMainTrueInMainThreadInLua52Plus
-- Test targets Lua 5.2+
local _, isMain = coroutine.running()
                return isMain
