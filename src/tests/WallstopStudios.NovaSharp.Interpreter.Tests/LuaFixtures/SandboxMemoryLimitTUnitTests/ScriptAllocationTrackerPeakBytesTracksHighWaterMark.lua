-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:633
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerPeakBytesTracksHighWaterMark
-- @compat-notes: Lua 5.3+: bitwise operators
local t = {}
                for i = 1, 100 do
                    t[i] = { value = i }
                end
