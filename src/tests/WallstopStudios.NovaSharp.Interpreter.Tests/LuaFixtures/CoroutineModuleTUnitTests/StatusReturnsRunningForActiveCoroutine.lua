-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:125
-- @test: CoroutineModuleTUnitTests.StatusReturnsRunningForActiveCoroutine
function queryRunningStatus()
                    local current = select(1, coroutine.running())
                    return coroutine.status(current)
                end
