-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:126
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
subject = {}
                setmetatable(subject, {
                    __metatable = 'locked'
                })
