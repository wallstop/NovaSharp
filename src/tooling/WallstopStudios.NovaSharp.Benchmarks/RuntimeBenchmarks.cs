namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// BenchmarkDotNet suite that executes representative NovaSharp runtime scenarios.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class RuntimeBenchmarks
    {
        private Script _script;
        private DynValue _compiledEntry = DynValue.Nil;
        private Func<double> _scenarioRunner;
        private BenchmarkHost _host = new();

        /// <summary>
        /// Scenario that will be executed for the next benchmark iteration.
        /// </summary>
        [Params(
            nameof(RuntimeScenario.NumericLoops),
            nameof(RuntimeScenario.TableMutation),
            nameof(RuntimeScenario.CoroutinePipeline),
            nameof(RuntimeScenario.UserDataInterop)
        )]
        public string ScenarioName { get; set; } = nameof(RuntimeScenario.NumericLoops);

        private RuntimeScenario CurrentScenario
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
                return Enum.Parse<RuntimeScenario>(ScenarioName, ignoreCase: false);
            }
        }

        [GlobalSetup]
        /// <summary>
        /// Compiles the scenario script and prepares the helpers before the benchmark run.
        /// </summary>
        public void Setup()
        {
            RuntimeScenario scenario = CurrentScenario;

            _script = new Script(CoreModulePresets.Complete);
            _compiledEntry = _script.LoadString(
                LuaRuntimeSuites.GetScript(scenario),
                null,
                $"scenario_{scenario}"
            );

            _scenarioRunner = scenario switch
            {
                RuntimeScenario.NumericLoops => () => _script.Call(_compiledEntry).Number,
                RuntimeScenario.TableMutation => RunTableScenario,
                RuntimeScenario.CoroutinePipeline => RunCoroutineScenario,
                RuntimeScenario.UserDataInterop => RunUserDataScenario,
                _ => () => _script.Call(_compiledEntry).Number,
            };

            if (
                scenario == RuntimeScenario.UserDataInterop
                && !UserData.IsTypeRegistered<BenchmarkHost>()
            )
            {
                UserData.RegisterType<BenchmarkHost>();
            }

            _host = new BenchmarkHost();
        }

        /// <summary>
        /// Executes the selected scenario and returns its numeric result.
        /// </summary>
        [Benchmark(Description = "Scenario Execution")]
        public double ExecuteScenario() => _scenarioRunner!();

        private double RunTableScenario()
        {
            Table table = new(_script);
            for (int i = 1; i <= LuaRuntimeSuites.TableEntryCount; i++)
            {
                table.Set(i, DynValue.NewNumber(i * 1.5));
            }

            return _script.Call(_compiledEntry, table).Number;
        }

        private double RunCoroutineScenario() =>
            _script.Call(_compiledEntry, LuaRuntimeSuites.CoroutineSteps).Number;

        private double RunUserDataScenario()
        {
            _host.Reset();
            return _script.Call(_compiledEntry, _host, LuaRuntimeSuites.UserDataIterations).Number;
        }
    }

    /// <summary>
    /// Host object exposed to Lua scripts for the userdata interop scenario.
    /// </summary>
    internal sealed class BenchmarkHost
    {
        private double _store;

        /// <summary>
        /// Sums the provided operands and caches the intermediate result.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>Computed value used inside the scenario.</returns>
        public double Accumulate(double left, double right)
        {
            double result = (left * 1.25) + (right * 0.75);
            _store = result;
            return result;
        }

        /// <summary>
        /// Persists the supplied value in the backing store.
        /// </summary>
        public void Store(double value) => _store = value;

        /// <summary>
        /// Gets the most recently stored value.
        /// </summary>
        public double Stored => _store;

        /// <summary>
        /// Resets the backing store to zero.
        /// </summary>
        public void Reset() => _store = 0;
    }
}
