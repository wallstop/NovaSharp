namespace WallstopStudios.NovaSharp.Comparison;

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using NLua;
using WallstopStudios.NovaSharp.Interpreter;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;
using WallstopStudios.NovaSharp.Interpreter.Modules;
using MoonCoreModules = MoonSharp.Interpreter.CoreModules;
using MoonDynValue = MoonSharp.Interpreter.DynValue;
using MoonScript = MoonSharp.Interpreter.Script;

/// <summary>
/// BenchmarkDotNet suite that compares NovaSharp, MoonSharp, and NLua compilation/execution throughput.
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
    private CompiledScript _novaSharpFunction;
    private MoonScript _moonSharpScript;
    private MoonDynValue _moonSharpFunction = MoonDynValue.Nil;
    private Lua _nLua;
    private LuaFunction _nLuaFunction;
    private bool _disposed;

    /// <summary>
    /// Scenario executed for each benchmark iteration.
    /// </summary>
    [Params(
        nameof(ScriptScenario.NumericLoops),
        nameof(ScriptScenario.TableMutation),
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
    /// Compiles the selected scenario for NovaSharp, MoonSharp, and NLua before the benchmarks run.
    /// </summary>
    public void Setup()
    {
        Script.WarmUp();
        MoonScript.WarmUp();

        _source = BenchmarkScripts.GetScript(CurrentScenario);

        _novaSharpScript = new Script(CoreModulePresets.Complete);
        _novaSharpFunction = _novaSharpScript.PrepareString(
            _source,
            null,
            $"precompiled_{CurrentScenario}"
        );

        _moonSharpScript = new MoonScript(MoonCoreModules.Preset_Complete);
        _moonSharpFunction = _moonSharpScript.LoadString(
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
    public CompiledScript NovaSharpCompile()
    {
        Script script = new(CoreModulePresets.Complete);
        return script.PrepareString(_source, null, $"compile_{CurrentScenario}");
    }

    [Benchmark(Description = "NovaSharp Execute")]
    /// <summary>
    /// Executes the previously compiled NovaSharp chunk.
    /// </summary>
    public DynValue NovaSharpExecute() => _novaSharpFunction.Execute();

    [Benchmark(Description = "MoonSharp Compile")]
    /// <summary>
    /// Compiles the scenario using MoonSharp.
    /// </summary>
    public MoonDynValue MoonSharpCompile()
    {
        MoonScript script = new(MoonCoreModules.Preset_Complete);
        return script.LoadString(_source, null, $"compile_{CurrentScenario}");
    }

    [Benchmark(Description = "MoonSharp Execute")]
    /// <summary>
    /// Executes the previously compiled MoonSharp chunk.
    /// </summary>
    public MoonDynValue MoonSharpExecute() => _moonSharpScript.Call(_moonSharpFunction);

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
