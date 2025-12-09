-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:712
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsClosureWithUpValues
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local x = 10
                local y = 20
                local z = 30
                function closure()
                    return x + y + z
                end
