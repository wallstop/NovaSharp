using BenchmarkDotNet.Attributes;
using NovaSharp.Interpreter;

namespace NovaSharp.Benchmarks;

[MemoryDiagnoser]
public class RuntimeBenchmarks
{
    private Script _script = null!;
    private DynValue _compiledEntry = DynValue.Nil;
    private Func<double>? _scenarioRunner;
    private BenchmarkHost _host = new();

    [Params(
        RuntimeScenario.NumericLoops,
        RuntimeScenario.TableMutation,
        RuntimeScenario.CoroutinePipeline,
        RuntimeScenario.UserDataInterop
    )]
    public RuntimeScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _script = new Script(CoreModules.Preset_Complete);
        _compiledEntry = _script.LoadString(
            LuaRuntimeSuites.GetScript(Scenario),
            null,
            $"scenario_{Scenario}"
        );

        _scenarioRunner = Scenario switch
        {
            RuntimeScenario.NumericLoops => () => _script.Call(_compiledEntry).Number,
            RuntimeScenario.TableMutation => RunTableScenario,
            RuntimeScenario.CoroutinePipeline => RunCoroutineScenario,
            RuntimeScenario.UserDataInterop => RunUserDataScenario,
            _ => () => _script.Call(_compiledEntry).Number,
        };

        if (
            Scenario == RuntimeScenario.UserDataInterop
            && !UserData.IsTypeRegistered(typeof(BenchmarkHost))
        )
        {
            UserData.RegisterType<BenchmarkHost>();
        }

        _host = new BenchmarkHost();
    }

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

internal sealed class BenchmarkHost
{
    private double _store;

    public double Accumulate(double left, double right) => (left * 1.25) + (right * 0.75);

    public void Store(double value) => _store = value;

    public double GetStored() => _store;

    public void Reset() => _store = 0;
}
