-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxMemoryLimitTUnitTests.cs:1127
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineLimitThrowsWhenExceeded
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
co2 = coroutine.create(function() end)
