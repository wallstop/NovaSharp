-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxInstructionLimitTUnitTests.cs:67
-- @test: SandboxInstructionLimitTUnitTests.UnlimitedInstructionsDoesNotThrow
-- @compat-notes: Test class 'SandboxInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local sum = 0
                for i = 1, 100 do
                    sum = sum + i
                end
                return sum
