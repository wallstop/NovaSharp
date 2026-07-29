-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:102
-- @test: VmStackCeilingTUnitTests.DeepBoundedRecursionSucceedsUnderDefaultCeiling
-- Recursion depth limits are implementation-defined; reference lua5.1 raises "stack overflow" at sum(20000) while 5.2+ and NovaSharp's default ceiling (~250k frames) do not
local function sum(n)
                    if n == 0 then return 0 end
                    return 1 + sum(n - 1)
                end
                return sum(20000)
