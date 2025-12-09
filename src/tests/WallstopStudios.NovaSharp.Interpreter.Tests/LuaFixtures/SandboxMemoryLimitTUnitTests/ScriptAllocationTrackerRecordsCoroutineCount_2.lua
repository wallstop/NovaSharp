-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1247
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsCoroutineCount
-- @compat-notes: Lua 5.3+: bitwise operators
co3 = coroutine.create(function() end)
