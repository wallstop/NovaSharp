-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:123
-- @test: CoroutineModuleTUnitTests.StatusReturnsNormalWhenInspectingMainFromChild
-- @compat-notes: Lua 5.3+: bitwise operators
local mainCoroutine = select(1, coroutine.running())

                function queryMainStatus()
                    return coroutine.status(mainCoroutine)
                end
