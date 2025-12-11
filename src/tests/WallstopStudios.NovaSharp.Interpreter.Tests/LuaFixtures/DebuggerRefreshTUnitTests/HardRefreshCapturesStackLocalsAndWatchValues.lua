-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Debugging\DebuggerRefreshTUnitTests.cs:21
-- @test: DebuggerRefreshTUnitTests.HardRefreshCapturesStackLocalsAndWatchValues
-- @compat-notes: Lua 5.3+: bitwise operators
sharedValue = 99
                function target()
                    local localValue = 42
                    local other = 'value'
                    return localValue + 1
                end
