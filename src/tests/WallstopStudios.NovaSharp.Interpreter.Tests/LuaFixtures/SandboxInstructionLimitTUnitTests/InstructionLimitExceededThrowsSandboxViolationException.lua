-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxInstructionLimitTUnitTests.cs:29
-- @test: SandboxInstructionLimitTUnitTests.InstructionLimitExceededThrowsSandboxViolationException
-- @compat-notes: Test class 'SandboxInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
while true do end
