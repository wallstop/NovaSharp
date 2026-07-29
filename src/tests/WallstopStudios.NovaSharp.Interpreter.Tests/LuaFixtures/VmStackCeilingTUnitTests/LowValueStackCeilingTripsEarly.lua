-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:124
-- @test: VmStackCeilingTUnitTests.LowValueStackCeilingTripsEarly
local function f(n) return 1 + f(n + 1) end
                        return f(0)
