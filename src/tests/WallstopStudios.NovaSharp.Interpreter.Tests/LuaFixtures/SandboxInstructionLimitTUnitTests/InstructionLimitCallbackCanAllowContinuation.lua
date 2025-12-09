-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxInstructionLimitTUnitTests.cs:87
-- @test: SandboxInstructionLimitTUnitTests.InstructionLimitCallbackCanAllowContinuation
-- @compat-notes: Test class 'SandboxInstructionLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local sum = 0
                    for i = 1, 1000 do
                        sum = sum + i
                    end
                    return sum
