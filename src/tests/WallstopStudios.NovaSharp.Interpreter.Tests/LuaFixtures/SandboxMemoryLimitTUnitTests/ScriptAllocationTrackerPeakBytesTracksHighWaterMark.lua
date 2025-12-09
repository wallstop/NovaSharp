-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxMemoryLimitTUnitTests.cs:633
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerPeakBytesTracksHighWaterMark
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local t = {}
                for i = 1, 100 do
                    t[i] = { value = i }
                end
