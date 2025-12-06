-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:59
-- @test: ProcessorCoroutineModuleTUnitTests.StatusReflectsLifecycleAndForceSuspendedStates
-- @compat-notes: Lua 5.3+: bitwise operators
function compute()
                    local sum = 0
                    for i = 1, 200 do
                        sum = sum + i
                    end
                    return sum
                end
