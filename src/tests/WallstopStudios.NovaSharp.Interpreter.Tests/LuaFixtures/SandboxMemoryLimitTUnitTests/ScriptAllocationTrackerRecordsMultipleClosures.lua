-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:741
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsMultipleClosures
function f1() return 1 end
                function f2() return 2 end
                function f3() return 3 end
                function f4() return 4 end
                function f5() return 5 end
