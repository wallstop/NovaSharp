-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:392
-- @test: OsSystemModuleTUnitTests.ClockReturnsMonotonicValues
-- @compat-notes: Lua 5.3+: bitwise operators
local values = {{}}
                for i = 1, {sampleCount} do
                    values[i] = os.clock()
                end
                return table.unpack(values)
