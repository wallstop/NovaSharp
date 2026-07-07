namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;

    /// <summary>
    /// Recursive Lua workloads used by <see cref="RecursiveAllocationBenchmarks"/>.
    /// </summary>
    internal enum RecursiveAllocationScenario
    {
        /// <summary>
        /// Recursive Fibonacci with enough calls to expose scalar wrapper allocation.
        /// </summary>
        Fibonacci20,

        /// <summary>
        /// Phase A0 recursive Fibonacci workload.
        /// </summary>
        Fibonacci30,

        /// <summary>
        /// Non-tail recursive call chain used to isolate ordinary call-frame allocation.
        /// </summary>
        NonTailDepth256,
    }

    /// <summary>
    /// Focused allocation probes for recursive Lua compute on precompiled chunks.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class RecursiveAllocationBenchmarks
    {
        private CompiledScript _compiled;

        /// <summary>
        /// Gets or sets the recursive workload selected by BenchmarkDotNet.
        /// </summary>
        [Params(
            nameof(RecursiveAllocationScenario.Fibonacci20),
            nameof(RecursiveAllocationScenario.Fibonacci30),
            nameof(RecursiveAllocationScenario.NonTailDepth256)
        )]
        public string ScenarioName { get; set; } = nameof(RecursiveAllocationScenario.Fibonacci20);

        private RecursiveAllocationScenario CurrentScenario
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
                return Enum.Parse<RecursiveAllocationScenario>(ScenarioName, ignoreCase: false);
            }
        }

        /// <summary>
        /// Compiles the selected chunk once and prepares the returned Lua function for repeated calls.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            RecursiveAllocationScenario scenario = CurrentScenario;
            Script script = new();
            CompiledScript chunk = script.PrepareString(
                GetScript(scenario),
                null,
                $"recursive_alloc_{scenario}"
            );
            DynValue function = chunk.Execute();
            _compiled = script.PrepareCallable(function);
        }

        /// <summary>
        /// Executes the prepared recursive Lua callable.
        /// </summary>
        /// <returns>The scalar result returned by the recursive Lua workload.</returns>
        [Benchmark(Description = "Precompiled recursive execute")]
        public DynValue ExecutePrecompiled() => _compiled.Execute();

        private static string GetScript(RecursiveAllocationScenario scenario) =>
            scenario switch
            {
                RecursiveAllocationScenario.Fibonacci20 => Fibonacci20,
                RecursiveAllocationScenario.Fibonacci30 => Fibonacci30,
                RecursiveAllocationScenario.NonTailDepth256 => NonTailDepth256,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(scenario),
                    scenario,
                    "Unknown recursive allocation scenario."
                ),
            };

        private const string Fibonacci20 = """
            local function fib(n)
                if n < 2 then
                    return n
                end
                return fib(n - 1) + fib(n - 2)
            end

            return function()
                return fib(20)
            end
            """;

        private const string Fibonacci30 = """
            local function fib(n)
                if n < 2 then
                    return n
                end
                return fib(n - 1) + fib(n - 2)
            end

            return function()
                return fib(30)
            end
            """;

        private const string NonTailDepth256 = """
            local function descend(n)
                if n == 0 then
                    return 0
                end
                return descend(n - 1) + 1
            end

            return function()
                return descend(256)
            end
            """;
    }
}
