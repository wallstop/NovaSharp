-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/PrecompiledRecursiveCallAllocationTUnitTests.cs
-- @test: PrecompiledRecursiveCallAllocationTUnitTests.PrecompiledFibonacci20AllocationsStayWithinCurrentRedBudget

local function fib(n)
    if n < 2 then
        return n
    end
    return fib(n - 1) + fib(n - 2)
end

assert(fib(20) == 6765, "fib(20) should return 6765")
print("PASS")
