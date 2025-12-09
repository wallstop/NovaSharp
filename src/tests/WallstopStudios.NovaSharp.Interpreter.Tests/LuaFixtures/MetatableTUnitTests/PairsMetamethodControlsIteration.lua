-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\MetatableTUnitTests.cs:87
-- @test: MetatableTUnitTests.PairsMetamethodControlsIteration
-- @compat-notes: Lua 5.3+: bitwise operators
subject = setmetatable({}, {
                    __pairs = function(self)
                        local yielded = false
                        return function(_, state)
                            if yielded then
                                return nil
                            end
                            yielded = true
                            return 'virtual', 42
                        end, self, nil
                    end
                })

                subject.a = 1
                collected = {}
                for k, v in pairs(subject) do
                    table.insert(collected, k .. '=' .. v)
                end
