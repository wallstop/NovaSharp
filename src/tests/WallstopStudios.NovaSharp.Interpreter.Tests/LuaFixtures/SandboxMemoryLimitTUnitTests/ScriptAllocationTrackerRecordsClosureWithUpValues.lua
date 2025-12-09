-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:712
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerRecordsClosureWithUpValues
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 10
                local y = 20
                local z = 30
                function closure()
                    return x + y + z
                end
