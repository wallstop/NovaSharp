-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxMemoryLimitTUnitTests.cs:1521
-- @test: SandboxMemoryLimitTUnitTests.ScriptCoroutineWrapCountsAgainstLimit
-- Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
f3 = coroutine.wrap(function() return 3 end)
