-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxRecursionLimitTUnitTests.cs:202
-- @test: SandboxRecursionLimitTUnitTests.MutualRecursionCountedCorrectly
-- Test class 'SandboxRecursionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return isEven(200)
