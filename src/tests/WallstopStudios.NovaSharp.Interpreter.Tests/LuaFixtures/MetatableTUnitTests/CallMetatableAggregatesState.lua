-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\MetatableTUnitTests.cs:62
-- @test: MetatableTUnitTests.CallMetatableAggregatesState
-- @compat-notes: Lua 5.3+: bitwise operators
subject = setmetatable({ total = 0 }, {
                    __call = function(self, amount)
                        self.total = self.total + amount
                        return self.total
                    end
                })
