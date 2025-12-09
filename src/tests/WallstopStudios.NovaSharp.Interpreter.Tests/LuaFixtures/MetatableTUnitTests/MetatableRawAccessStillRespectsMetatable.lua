-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\MetatableTUnitTests.cs:37
-- @test: MetatableTUnitTests.MetatableRawAccessStillRespectsMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
subject = {}
                setmetatable(subject, {
                    __newindex = function(t, key, value)
                        rawset(t, key, value * 2)
                    end
                })

                subject.value = 5
