-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/MetatableTUnitTests.cs:37
-- @test: MetatableTUnitTests.MetatableRawAccessStillRespectsMetatable
-- @compat-notes: Lua 5.3+: bitwise operators
subject = {}
                setmetatable(subject, {
                    __newindex = function(t, key, value)
                        rawset(t, key, value * 2)
                    end
                })

                subject.value = 5
