-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:102
-- @test: VmStackCeilingTUnitTests.DeepBoundedRecursionSucceedsUnderDefaultCeiling
local function sum(n)
                    if n == 0 then return 0 end
                    return 1 + sum(n - 1)
                end
                return sum(20000)
