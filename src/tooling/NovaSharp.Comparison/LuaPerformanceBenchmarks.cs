namespace NovaSharp.Comparison;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using NLua;
using NovaSharp.Interpreter;
using NovaSharp.Interpreter.DataTypes;
using NovaSharp.Interpreter.Modules;

#nullable enable

/// <summary>
/// BenchmarkDotNet suite that compares NovaSharp and NLua compilation/execution throughput.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
[SuppressMessage(
    "Performance",
    "CA1812",
    Justification = "Instantiated via BenchmarkDotNet reflection."
)]
internal sealed class LuaPerformanceBenchmarks : IDisposable
{
    private string _source = string.Empty;
    private Script _novaSharpScript = null!;
    private DynValue _novaSharpFunction = DynValue.Nil;
    private Lua _nLua = null!;
    private LuaFunction? _nLuaFunction;
    private bool _disposed;

    /// <summary>
    /// Scenario executed for each benchmark iteration.
    /// </summary>
    [Params(
        ScriptScenario.TowerOfHanoi,
        ScriptScenario.EightQueens,
        ScriptScenario.CoroutinePingPong
    )]
    public ScriptScenario Scenario { get; set; }

    [GlobalSetup]
    /// <summary>
    /// Compiles the selected scenario for both NovaSharp and NLua before the benchmarks run.
    /// </summary>
    public void Setup()
    {
        Script.WarmUp();

        _source = BenchmarkScripts.GetScript(Scenario);

        _novaSharpScript = new Script(CoreModules.PresetComplete);
        _novaSharpFunction = _novaSharpScript.LoadString(_source, null, $"precompiled_{Scenario}");

        _nLua = new Lua();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{Scenario}") as LuaFunction;
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
        Script script = new(CoreModules.PresetComplete);
        return script.LoadString(_source, null, $"compile_{Scenario}");
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
        LuaFunction? compiled = _nLua.LoadString(_source, $"compile_{Scenario}") as LuaFunction;
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
    public object? NLuaExecute()
    {
        if (_nLuaFunction == null)
        {
            return null;
        }

        object? result = null;
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _nLuaFunction?.Dispose();
        _nLua?.Dispose();
    }
}
