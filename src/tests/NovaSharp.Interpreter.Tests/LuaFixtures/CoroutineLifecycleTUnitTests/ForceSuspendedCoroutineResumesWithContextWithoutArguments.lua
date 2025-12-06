-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:161
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineResumesWithContextWithoutArguments
-- @compat-notes: Lua 5.3+: bitwise operators
function heavyweight()
                    local total = 0
                    for i = 1, 300 do
                        total = total + i
                    end
                    return total
                end
