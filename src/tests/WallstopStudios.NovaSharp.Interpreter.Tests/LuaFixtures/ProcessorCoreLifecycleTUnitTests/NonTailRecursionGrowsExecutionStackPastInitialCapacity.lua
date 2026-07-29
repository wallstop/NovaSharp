-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoreLifecycleTUnitTests.cs:294
-- @test: ProcessorCoreLifecycleTUnitTests.NonTailRecursionGrowsExecutionStackPastInitialCapacity
local function recurse(n)
    if n == 0 then
        return 1
    end

    return 1 + recurse(n - 1)
end

assert(recurse(80) == 81)
