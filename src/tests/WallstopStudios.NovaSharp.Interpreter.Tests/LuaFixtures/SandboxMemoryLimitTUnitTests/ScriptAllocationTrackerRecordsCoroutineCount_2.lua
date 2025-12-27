-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1432
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsCoroutineCount
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
co3 = coroutine.create(function() end)
