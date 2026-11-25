-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxTableIterationInstructionLimitTUnitTests.cs:65
-- @test: SandboxTableIterationInstructionLimitTUnitTests.NativeWorkDuringRenewalCannotRenewRecursively
-- Compatibility notes: Test class 'SandboxTableIterationInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
return function(visit, values, action) visit(values, action) end
