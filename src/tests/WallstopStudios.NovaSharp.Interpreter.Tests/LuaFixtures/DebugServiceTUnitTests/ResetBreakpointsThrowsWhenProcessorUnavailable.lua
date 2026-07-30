-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Debugging/DebugServiceTUnitTests.cs:109
-- @test: DebugServiceTUnitTests.ResetBreakpointsThrowsWhenProcessorUnavailable
local a = 1
            local b = 2
            if a < b then
                return b - a
            end
            return a + b
