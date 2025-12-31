-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:303
-- @test: DebugModuleTapParityTUnitTests.UpvalueJoinSharesState
-- @compat-notes: Lua 5.2+: debug.upvalueid (5.2+); Lua 5.2+: debug.upvaluejoin (5.2+)
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
