-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxInstructionLimitTUnitTests.cs:22
-- @test: SandboxInstructionLimitTUnitTests.InstructionLimitExceededThrowsSandboxViolationException
while true do end
