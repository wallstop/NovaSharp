-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:798
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsMultipleCoroutines
-- @compat-notes: Lua 5.3+: bitwise operators
function gen()
                    coroutine.yield(1)
                end
                co1 = coroutine.create(gen)
                co2 = coroutine.create(gen)
                co3 = coroutine.create(gen)
