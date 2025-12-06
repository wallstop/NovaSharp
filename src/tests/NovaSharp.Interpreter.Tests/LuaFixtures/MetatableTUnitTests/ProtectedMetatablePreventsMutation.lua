-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/MetatableTUnitTests.cs:119
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
-- @compat-notes: Lua 5.3+: bitwise operators
subject = {}
                setmetatable(subject, {
                    __metatable = 'locked'
                })
