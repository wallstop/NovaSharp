-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoreLifecycleTUnitTests.cs:323
-- @test: ProcessorCoreLifecycleTUnitTests.LargeVarargCallGrowsValueStackPastInitialCapacity
local function count(...)
    return select('#', ...)
end

local function values(n)
    if n == 0 then
        return
    end

    return n, values(n - 1)
end

assert(count(values(528)) == 528)
