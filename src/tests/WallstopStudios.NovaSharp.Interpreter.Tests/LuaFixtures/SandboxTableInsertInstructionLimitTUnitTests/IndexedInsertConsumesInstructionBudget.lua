-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxTableInsertInstructionLimitTUnitTests.cs:23
-- @test: SandboxTableInsertInstructionLimitTUnitTests.IndexedInsertConsumesInstructionBudget
-- Compatibility notes: Test class 'SandboxTableInsertInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
table.insert(values, 1, 'new')
