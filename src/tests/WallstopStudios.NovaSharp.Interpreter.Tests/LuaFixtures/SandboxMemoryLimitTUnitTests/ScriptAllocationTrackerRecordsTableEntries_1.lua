-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:542
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsTableEntries
-- @compat-notes: Lua 5.3+: bitwise operators
for i = 1, 10 do t[i] = i end
