-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Debugging/DebuggerBreakpointTUnitTests.cs:25
-- @test: DebuggerBreakpointTUnitTests.DebuggerActionsUpdateBreakpointsAndRefreshes
sharedValue = 3
                function target()
                    local localValue = 2
                    return localValue + sharedValue
                end
