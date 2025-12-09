-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1300
-- @test: SandboxMemoryLimitTUnitTests.ScriptWithoutCoroutineLimitAllowsUnlimited
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local coros = {}
                for i = 1, 100 do
                    coros[i] = coroutine.create(function() end)
                end
