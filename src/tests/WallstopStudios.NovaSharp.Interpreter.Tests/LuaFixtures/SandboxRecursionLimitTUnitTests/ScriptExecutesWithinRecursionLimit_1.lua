-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxRecursionLimitTUnitTests.cs:63
-- @test: SandboxRecursionLimitTUnitTests.ScriptExecutesWithinRecursionLimit
return factorial(10)
