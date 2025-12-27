-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1302
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineLimitThrowsWhenExceeded
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
co1 = coroutine.create(function() end)
