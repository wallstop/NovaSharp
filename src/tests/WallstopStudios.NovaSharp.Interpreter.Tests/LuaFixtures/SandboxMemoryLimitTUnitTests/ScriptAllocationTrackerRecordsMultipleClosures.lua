-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1044
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsMultipleClosures
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
function f1() return 1 end
                function f2() return 2 end
                function f3() return 3 end
                function f4() return 4 end
                function f5() return 5 end
