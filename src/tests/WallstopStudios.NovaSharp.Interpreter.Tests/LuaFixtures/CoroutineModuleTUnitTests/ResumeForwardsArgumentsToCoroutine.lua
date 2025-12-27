-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:453
-- @test: CoroutineModuleTUnitTests.ResumeForwardsArgumentsToCoroutine
function sum(...)
                    local total = 0
                    for i = 1, select('#', ...) do
                        total = total + select(i, ...)
                    end
                    return total
                end
