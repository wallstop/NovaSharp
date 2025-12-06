-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:367
-- @test: CoroutineModuleTUnitTests.ResumeForwardsArgumentsToCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function sum(...)
                    local total = 0
                    for i = 1, select('#', ...) do
                        total = total + select(i, ...)
                    end
                    return total
                end
