namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class PrecompiledRecursiveCallAllocationTUnitTests
    {
        private const long Fibonacci20CurrentRedBudgetBytesPerCall = 24L * 1024L * 1024L;
        private const long NonTailCurrentRedBudgetBytesPerCall = 2L * 1024L * 1024L;
        private const int AllocationSmokeIterations = 2;

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PrecompiledFibonacci20AllocationsStayWithinCurrentRedBudget(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            CompiledScript recursive = PrepareRecursiveFunction(
                script,
                FibonacciCallableSource,
                "recursive_alloc_fib20.lua"
            );
            DynValue input = DynValue.NewNumber(20);

            MeasureAllocations(recursive, input, expectedNumber: 6765d, iterations: 1);
            ForceFullCollection();

            long allocated = MeasureAllocations(
                recursive,
                input,
                expectedNumber: 6765d,
                AllocationSmokeIterations
            );
            long allocatedPerCall = allocated / AllocationSmokeIterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(Fibonacci20CurrentRedBudgetBytesPerCall)
                .Because(
                    $"Precompiled fib(20) allocated {allocated} bytes across {AllocationSmokeIterations} iterations ({allocatedPerCall} bytes/call). Ratchet this toward the A1 target after LuaValue removes scalar wrapper allocation."
                )
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task PrecompiledNonTailRecursiveCallAllocationsStayWithinCurrentRedBudget(
            LuaCompatibilityVersion version
        )
        {
            Script script = new(version);
            CompiledScript recursive = PrepareRecursiveFunction(
                script,
                NonTailCallableSource,
                "recursive_alloc_nontail.lua"
            );
            DynValue input = DynValue.NewNumber(256);

            MeasureAllocations(recursive, input, expectedNumber: 256d, iterations: 1);
            ForceFullCollection();

            long allocated = MeasureAllocations(
                recursive,
                input,
                expectedNumber: 256d,
                AllocationSmokeIterations
            );
            long allocatedPerCall = allocated / AllocationSmokeIterations;

            await Assert
                .That(allocatedPerCall)
                .IsLessThan(NonTailCurrentRedBudgetBytesPerCall)
                .Because(
                    $"Precompiled non-tail recursion allocated {allocated} bytes across {AllocationSmokeIterations} iterations ({allocatedPerCall} bytes/call). Ratchet this toward the A5 0 B steady-state target after stack-window calls land."
                )
                .ConfigureAwait(false);
        }

        private static CompiledScript PrepareRecursiveFunction(
            Script script,
            string source,
            string chunkName
        )
        {
            CompiledScript chunk = script.PrepareString(source, null, chunkName);
            DynValue function = chunk.Execute();
            return script.PrepareCallable(function);
        }

        private static long MeasureAllocations(
            CompiledScript recursive,
            DynValue input,
            double expectedNumber,
            int iterations
        )
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++)
            {
                DynValue result = recursive.Execute(input);
                if (result.Number != expectedNumber)
                {
                    throw new InvalidOperationException(
                        "Recursive allocation probe returned an unexpected value."
                    );
                }
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private const string FibonacciCallableSource = """
            local function fib(n)
                if n < 2 then
                    return n
                end
                return fib(n - 1) + fib(n - 2)
            end

            return function(n)
                return fib(n)
            end
            """;

        private const string NonTailCallableSource = """
            local function descend(n)
                if n == 0 then
                    return 0
                end
                return descend(n - 1) + 1
            end

            return function(n)
                return descend(n)
            end
            """;
    }
}
