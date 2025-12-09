-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1320
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineWrapCountsAgainstLimit
-- @compat-notes: Lua 5.3+: bitwise operators
f1 = coroutine.wrap(function() return 1 end)
