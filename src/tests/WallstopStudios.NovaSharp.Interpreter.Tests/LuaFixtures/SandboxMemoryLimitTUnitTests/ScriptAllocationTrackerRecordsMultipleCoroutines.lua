-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:798
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsMultipleCoroutines
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
function gen()
                    coroutine.yield(1)
                end
                co1 = coroutine.create(gen)
                co2 = coroutine.create(gen)
                co3 = coroutine.create(gen)
