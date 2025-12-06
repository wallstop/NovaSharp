-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DebuggerBreakpointTUnitTests.cs:20
-- @test: DebuggerBreakpointTUnitTests.DebuggerActionsUpdateBreakpointsAndRefreshes
-- @compat-notes: Lua 5.3+: bitwise operators
sharedValue = 3
                function target()
                    local localValue = 2
                    return localValue + sharedValue
                end
