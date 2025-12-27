-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:1382
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineLimitCallbackCanDenyContinuation
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
co1 = coroutine.create(function() end)
