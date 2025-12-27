-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:67
-- @test: MetatableTUnitTests.CallMetatableAggregatesState
subject = setmetatable({ total = 0 }, {
                    __call = function(self, amount)
                        self.total = self.total + amount
                        return self.total
                    end
                })
