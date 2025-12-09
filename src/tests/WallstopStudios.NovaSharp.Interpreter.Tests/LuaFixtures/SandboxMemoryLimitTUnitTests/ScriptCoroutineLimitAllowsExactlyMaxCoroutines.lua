-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1273
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineLimitAllowsExactlyMaxCoroutines
-- @compat-notes: Lua 5.3+: bitwise operators
co1 = coroutine.create(function() end)
                co2 = coroutine.create(function() end)
                co3 = coroutine.create(function() end)
