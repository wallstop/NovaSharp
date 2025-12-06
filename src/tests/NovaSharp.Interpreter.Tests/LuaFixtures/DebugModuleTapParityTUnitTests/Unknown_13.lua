-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:285
-- @test: DebugModuleTapParityTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local function counter(start)
                    local value = start
                    return function(delta)
                        if delta ~= nil then
                            value = value + delta
                        end
                        return value
                    end
                end

                local first = counter(0)
                local second = counter(100)
                local beforeShared = debug.upvalueid(first, 2) == debug.upvalueid(second, 2)
                debug.upvaluejoin(second, 2, first, 2)
                local afterShared = debug.upvalueid(first, 2) == debug.upvalueid(second, 2)
                second(5)
                local firstValue = first()
                local secondValue = second()

                return {
                    before = beforeShared,
                    after = afterShared,
                    firstValue = firstValue,
                    secondValue = secondValue
                }
