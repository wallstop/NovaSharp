-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1362
-- @test: CoroutineModuleTUnitTests.RunningReturnsIsMainTrueInMainThreadInLua52Plus
-- @compat-notes: Test targets Lua 5.2+
local _, isMain = coroutine.running()
                return isMain
