-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/PrecompiledRecursiveCallAllocationTUnitTests.cs
-- @test: PrecompiledRecursiveCallAllocationTUnitTests.PrecompiledNonTailRecursiveCallAllocationsStayWithinCurrentRedBudget

local function descend(n)
    if n == 0 then
        return 0
    end
    return descend(n - 1) + 1
end

assert(descend(256) == 256, "non-tail recursion should return its depth")
print("PASS")
