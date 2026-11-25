-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxTableIterationInstructionLimitTUnitTests.cs:27
-- @test: SandboxTableIterationInstructionLimitTUnitTests.LegacyIterationConsumesInstructionBudget
-- Compatibility notes: Test class 'SandboxTableIterationInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
iterate(values, function() end)
