-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxRecursionLimitTUnitTests.cs:87
-- @test: SandboxRecursionLimitTUnitTests.UnlimitedRecursionDoesNotThrow
-- @compat-notes: Test class 'SandboxRecursionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return recurse(50)
