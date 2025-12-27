-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:232
-- @test: CoroutineTUnitTests.CoroutineSupportsTypedEnumerable
return function()
                    local x = 0
                    while true do
                        x = x + 1
                        coroutine.yield(x)
                        if (x > 5) then
                            return 7
                        end
                    end
                end
