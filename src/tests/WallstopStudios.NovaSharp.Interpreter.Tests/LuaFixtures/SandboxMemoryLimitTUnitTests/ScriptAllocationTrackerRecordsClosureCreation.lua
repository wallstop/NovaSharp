-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:692
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsClosureCreation
function myFunc() return 42 end
