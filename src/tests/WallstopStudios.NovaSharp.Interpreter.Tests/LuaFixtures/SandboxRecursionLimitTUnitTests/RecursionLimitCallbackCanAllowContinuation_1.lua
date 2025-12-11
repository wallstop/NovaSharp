-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxRecursionLimitTUnitTests.cs:122
-- @test: SandboxRecursionLimitTUnitTests.RecursionLimitCallbackCanAllowContinuation
-- @compat-notes: Test class 'SandboxRecursionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return recurse(100)
