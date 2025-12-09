-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxInstructionLimitTUnitTests.cs:41
-- @test: SandboxInstructionLimitTUnitTests.ScriptExecutesWithinInstructionLimit
-- @compat-notes: Test class 'SandboxInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return 1 + 2
