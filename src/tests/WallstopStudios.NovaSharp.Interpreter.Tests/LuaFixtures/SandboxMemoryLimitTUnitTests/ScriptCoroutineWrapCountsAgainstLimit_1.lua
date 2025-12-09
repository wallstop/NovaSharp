-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1321
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineWrapCountsAgainstLimit
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
f2 = coroutine.wrap(function() return 2 end)
