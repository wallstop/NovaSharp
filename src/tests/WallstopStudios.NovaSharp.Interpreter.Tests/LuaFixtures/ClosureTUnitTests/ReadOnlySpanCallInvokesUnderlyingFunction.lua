-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:346
-- @test: ClosureTUnitTests.ReadOnlySpanCallInvokesUnderlyingFunction
return function(...)
                    local sum = 0
                    for i = 1, select('#', ...) do
                        sum = sum + select(i, ...)
                    end
                    return sum
                end
