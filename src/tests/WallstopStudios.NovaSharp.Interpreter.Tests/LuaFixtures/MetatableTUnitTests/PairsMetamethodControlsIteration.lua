-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:93
-- @test: MetatableTUnitTests.PairsMetamethodControlsIteration
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
