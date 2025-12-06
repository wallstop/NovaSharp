-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:229
-- @test: ProcessorCoroutineCloseTUnitTests.CloseForceSuspendedCoroutineUnwindsSuccessfully
-- @compat-notes: Lua 5.3+: bitwise operators
function heavy_close()
                    local total = 0
                    for i = 1, 500 do
                        total = total + i
                    end
                    return total
                end
