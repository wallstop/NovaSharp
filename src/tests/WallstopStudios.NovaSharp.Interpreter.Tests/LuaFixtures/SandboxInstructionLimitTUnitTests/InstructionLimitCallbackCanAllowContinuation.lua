-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxInstructionLimitTUnitTests.cs:87
-- @test: SandboxInstructionLimitTUnitTests.InstructionLimitCallbackCanAllowContinuation
-- @compat-notes: Lua 5.3+: bitwise operators
local sum = 0
                    for i = 1, 1000 do
                        sum = sum + i
                    end
                    return sum
