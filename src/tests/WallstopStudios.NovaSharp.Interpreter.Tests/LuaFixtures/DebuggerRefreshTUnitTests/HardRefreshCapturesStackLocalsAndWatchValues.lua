-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Debugging/DebuggerRefreshTUnitTests.cs:26
-- @test: DebuggerRefreshTUnitTests.HardRefreshCapturesStackLocalsAndWatchValues
sharedValue = 99
                function target()
                    local localValue = 42
                    local other = 'value'
                    return localValue + 1
                end
