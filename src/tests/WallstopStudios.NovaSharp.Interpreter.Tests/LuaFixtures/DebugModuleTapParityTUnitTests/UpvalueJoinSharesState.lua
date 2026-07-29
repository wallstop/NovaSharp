-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:312
-- @test: DebugModuleTapParityTUnitTests.UpvalueJoinSharesState
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
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
                local beforeShared = debug.upvalueid(first, {valueIndex}) == debug.upvalueid(second, {valueIndex})
                debug.upvaluejoin(second, {valueIndex}, first, {valueIndex})
                local afterShared = debug.upvalueid(first, {valueIndex}) == debug.upvalueid(second, {valueIndex})
                second(5)
                local firstValue = first()
                local secondValue = second()

                return {{
                    before = beforeShared,
                    after = afterShared,
                    firstValue = firstValue,
                    secondValue = secondValue
                }}
