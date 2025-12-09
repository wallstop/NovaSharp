-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1321
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineWrapCountsAgainstLimit
-- @compat-notes: Lua 5.3+: bitwise operators
f2 = coroutine.wrap(function() return 2 end)
