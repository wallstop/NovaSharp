-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:552
-- @test: OsSystemModuleTUnitTests.ClockReturnsMonotonicValues
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
local values = {{}}
                for i = 1, {sampleCount} do
                    values[i] = os.clock()
                end
                return table.unpack(values)
