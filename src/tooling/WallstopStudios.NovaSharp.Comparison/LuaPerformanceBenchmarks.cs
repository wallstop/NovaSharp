namespace WallstopStudios.NovaSharp.Comparison;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using NLua;
using WallstopStudios.NovaSharp.Interpreter;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;
using WallstopStudios.NovaSharp.Interpreter.Modules;

/// <summary>
/// BenchmarkDotNet suite that compares NovaSharp and NLua compilation/execution throughput.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
[SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "BenchmarkDotNet comparison benchmarks must remain public and unsealed."
)]
public class LuaPerformanceBenchmarks : IDisposable
{
    private string _source = string.Empty;
    private Script _novaSharpScript;
    private DynValue _novaSharpFunction = DynValue.Nil;
    private Lua _nLua;
    private LuaFunction _nLuaFunction;
    private bool _disposed;

    /// <summary>
    /// Scenario executed for each benchmark iteration.
    /// </summary>
    [Params(
        nameof(ScriptScenario.TowerOfHanoi),
        nameof(ScriptScenario.EightQueens),
        nameof(ScriptScenario.CoroutinePingPong)
    )]
    public string ScenarioName { get; set; } = nameof(ScriptScenario.TowerOfHanoi);

    private ScriptScenario CurrentScenario
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
            return Enum.Parse<ScriptScenario>(ScenarioName, ignoreCase: false);
        }
    }

    [GlobalSetup]
    /// <summary>
    /// Compiles the selected scenario for both NovaSharp and NLua before the benchmarks run.
    /// </summary>
    public void Setup()
    {
        Script.WarmUp();

        _source = BenchmarkScripts.GetScript(CurrentScenario);

        _novaSharpScript = new Script(CoreModulePresets.Complete);
        _novaSharpFunction = _novaSharpScript.LoadString(
            _source,
            null,
            $"precompiled_{CurrentScenario}"
        );

        _nLua = new Lua();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{CurrentScenario}") as LuaFunction;
    }

    [GlobalCleanup]
    /// <summary>
    /// Releases NLua resources after the benchmark run.
    /// </summary>
    public void Cleanup() => Dispose();

    [Benchmark(Description = "NovaSharp Compile")]
    /// <summary>
    /// Compiles the scenario using a fresh NovaSharp script instance.
    /// </summary>
    public DynValue NovaSharpCompile()
    {
        Script script = new(CoreModulePresets.Complete);
        return script.LoadString(_source, null, $"compile_{CurrentScenario}");
    }

    [Benchmark(Description = "NovaSharp Execute")]
    /// <summary>
    /// Executes the previously compiled NovaSharp chunk.
    /// </summary>
    public DynValue NovaSharpExecute() => _novaSharpScript.Call(_novaSharpFunction);

    [Benchmark(Description = "NLua Compile")]
    /// <summary>
    /// Compiles the scenario using NLua.
    /// </summary>
    public LuaFunction NLuaCompile()
    {
        LuaFunction compiled =
            _nLua.LoadString(_source, $"compile_{CurrentScenario}") as LuaFunction;
        if (compiled == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }

        return compiled;
    }

    [Benchmark(Description = "NLua Execute")]
    /// <summary>
    /// Executes the previously compiled NLua chunk.
    /// </summary>
    public object NLuaExecute()
    {
        if (_nLuaFunction == null)
        {
            return null;
        }

        object result = null;
        foreach (object value in _nLuaFunction.Call())
        {
            result = value;
        }
        return result;
    }

    /// <summary>
    /// Disposes NLua resources created by the benchmark.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _nLuaFunction?.Dispose();
            _nLua?.Dispose();
        }

        _disposed = true;
    }
}
