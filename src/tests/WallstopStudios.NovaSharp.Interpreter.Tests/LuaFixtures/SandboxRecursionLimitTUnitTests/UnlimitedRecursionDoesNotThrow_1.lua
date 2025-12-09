-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:87
-- @test: SandboxRecursionLimitTUnitTests.UnlimitedRecursionDoesNotThrow
return recurse(50)
