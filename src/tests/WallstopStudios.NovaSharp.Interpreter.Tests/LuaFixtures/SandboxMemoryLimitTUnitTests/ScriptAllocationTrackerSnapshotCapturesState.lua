-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:772
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerSnapshotCapturesState
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
t = { a = 1, b = 2, c = 3 }
