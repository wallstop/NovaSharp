-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxTableInsertInstructionLimitTUnitTests.cs:150
-- @test: SandboxTableInsertInstructionLimitTUnitTests.DirectInsertCountsLengthCallbackAndIndexedWritesTogether
-- Compatibility notes: Test class 'SandboxTableInsertInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return function(callback) callback() end
