-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\MetatableTUnitTests.cs:119
-- @test: MetatableTUnitTests.ProtectedMetatablePreventsMutation
-- @compat-notes: Lua 5.3+: bitwise operators
subject = {}
                setmetatable(subject, {
                    __metatable = 'locked'
                })
