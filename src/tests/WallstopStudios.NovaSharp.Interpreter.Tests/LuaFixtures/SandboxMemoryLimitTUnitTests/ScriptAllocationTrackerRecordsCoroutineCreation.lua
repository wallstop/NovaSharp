-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:769
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsCoroutineCreation
-- @compat-notes: Lua 5.3+: bitwise operators
function myCoroutine()
                    coroutine.yield(1)
                    coroutine.yield(2)
                    return 3
                end
                co = coroutine.create(myCoroutine)
