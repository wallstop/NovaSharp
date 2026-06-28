-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:154
-- @test: CoroutineModuleTUnitTests.StatusReturnsNormalWhenInspectingMainFromChild
-- Test targets Lua 5.2+
local mainCoroutine = select(1, coroutine.running())

                function queryMainStatus()
                    return coroutine.status(mainCoroutine)
                end
