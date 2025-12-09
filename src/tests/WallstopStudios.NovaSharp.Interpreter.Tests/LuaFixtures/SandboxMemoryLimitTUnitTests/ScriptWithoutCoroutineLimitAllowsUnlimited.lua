-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1300
-- @test: SandboxMemoryLimitTUnitTests.ScriptWithoutCoroutineLimitAllowsUnlimited
-- @compat-notes: Lua 5.3+: bitwise operators
local coros = {}
                for i = 1, 100 do
                    coros[i] = coroutine.create(function() end)
                end
