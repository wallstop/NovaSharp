namespace WallstopStudios.NovaSharp.Comparison;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Lua;
using Lua.Runtime;
using Lua.Standard;
using WallstopStudios.NovaSharp.Interpreter;
using WallstopStudios.NovaSharp.Interpreter.DataTypes;
using WallstopStudios.NovaSharp.Interpreter.Modules;
using LuaCSharpValue = Lua.LuaValue;
using MoonCoreModules = MoonSharp.Interpreter.CoreModules;
using MoonDynValue = MoonSharp.Interpreter.DynValue;
using MoonScript = MoonSharp.Interpreter.Script;
using NLuaFunction = NLua.LuaFunction;
using NLuaState = NLua.Lua;

/// <summary>
/// BenchmarkDotNet suite that compares NovaSharp, MoonSharp, NLua, and Lua-CSharp compilation/execution throughput.
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
    private static readonly string[] BenchmarkScenarioNames = BenchmarkScripts.GetScenarioNames();

    private string _source = string.Empty;
    private Script _novaSharpScript;
    private CompiledScript _novaSharpFunction;
    private MoonScript _moonSharpScript;
    private MoonDynValue _moonSharpFunction = MoonDynValue.Nil;
    private NLuaState _nLua;
    private NLuaFunction _nLuaFunction;
    private LuaState _luaCSharpState;
    private LuaClosure _luaCSharpFunction;
    private bool _disposed;

    /// <summary>
    /// Scenario executed for each benchmark iteration.
    /// </summary>
    [ParamsSource(nameof(ScenarioNames))]
    public string ScenarioName { get; set; } = nameof(ScriptScenario.TowerOfHanoi);

    /// <summary>
    /// Scenarios executed by BenchmarkDotNet. Kept in sync with CLI scenario export.
    /// </summary>
    public static IEnumerable<string> ScenarioNames => BenchmarkScenarioNames;

    private ScriptScenario CurrentScenario
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(ScenarioName);
            return BenchmarkScripts.GetScenario(ScenarioName);
        }
    }

    [GlobalSetup]
    /// <summary>
    /// Compiles the selected scenario for NovaSharp, MoonSharp, NLua, and Lua-CSharp before the benchmarks run.
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

        _nLua = new NLuaState();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{CurrentScenario}") as NLuaFunction;
        if (_nLuaFunction == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }

        _luaCSharpState = CreateLuaCSharpState();
        _luaCSharpFunction = _luaCSharpState.Load(
            _source.AsSpan(),
            $"precompiled_{CurrentScenario}",
            _luaCSharpState.Environment
        );
    }

    [GlobalCleanup]
    /// <summary>
    /// Releases runtime resources after the benchmark run.
    /// </summary>
    public void Cleanup() => Dispose();

    [Benchmark(Description = "NovaSharp Compile")]
    /// <summary>
    /// Compiles the scenario using a fresh NovaSharp script instance.
    /// </summary>
    public int NovaSharpCompile()
    {
        Script script = new(CoreModulePresets.Complete);
        CompiledScript compiled = script.PrepareString(_source, null, $"compile_{CurrentScenario}");
        GC.KeepAlive(compiled.Function);
        return 1;
    }

    [Benchmark(Description = "NovaSharp Execute")]
    /// <summary>
    /// Executes the previously compiled NovaSharp chunk.
    /// </summary>
    public DynValue NovaSharpExecute() => _novaSharpFunction.Execute();

    [Benchmark(Description = "MoonSharp Compile")]
    /// <summary>
    /// Compiles the scenario using a fresh MoonSharp script instance.
    /// </summary>
    public int MoonSharpCompile()
    {
        MoonScript script = new(MoonCoreModules.Preset_Complete);
        MoonDynValue compiled = script.LoadString(_source, null, $"compile_{CurrentScenario}");
        GC.KeepAlive(compiled);
        return 1;
    }

    [Benchmark(Description = "MoonSharp Execute")]
    /// <summary>
    /// Executes the previously compiled MoonSharp chunk.
    /// </summary>
    public MoonDynValue MoonSharpExecute() => _moonSharpScript.Call(_moonSharpFunction);

    [Benchmark(Description = "NLua Compile")]
    /// <summary>
    /// Compiles the scenario using a fresh NLua state.
    /// </summary>
    public int NLuaCompile()
    {
        using NLuaState state = new();
        NLuaFunction compiled =
            state.LoadString(_source, $"compile_{CurrentScenario}") as NLuaFunction;
        if (compiled == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }

        GC.KeepAlive(compiled);
        return 1;
    }

    [Benchmark(Description = "LuaCSharp Compile")]
    /// <summary>
    /// Compiles the scenario using a fresh Lua-CSharp state.
    /// </summary>
    public int LuaCSharpCompile()
    {
        using LuaState state = CreateLuaCSharpState();
        LuaClosure compiled = state.Load(
            _source.AsSpan(),
            $"compile_{CurrentScenario}",
            state.Environment
        );
        GC.KeepAlive(compiled);
        return 1;
    }

    [Benchmark(Description = "LuaCSharp Execute")]
    /// <summary>
    /// Executes the previously compiled Lua-CSharp chunk.
    /// </summary>
    public LuaCSharpValue LuaCSharpExecute() => RunLuaCSharp(_luaCSharpState, _luaCSharpFunction);

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

    private static LuaState CreateLuaCSharpState()
    {
        LuaState state = LuaState.Create();
        state.OpenStandardLibraries();
        return state;
    }

    private static LuaCSharpValue RunLuaCSharp(LuaState state, LuaClosure closure)
    {
        int returnCount = GetLuaCSharpReturnCount(state.RunAsync(closure, CancellationToken.None));
        if (returnCount <= 0)
        {
            return LuaCSharpValue.Nil;
        }

        using LuaStackReader reader = state.ReadStack(returnCount);
        ReadOnlySpan<LuaCSharpValue> values = reader.AsSpan();
        return values.Length > 0 ? values[^1] : LuaCSharpValue.Nil;
    }

    private static int GetLuaCSharpReturnCount(ValueTask<int> runTask)
    {
        return runTask.ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes runtime resources created by the benchmark.
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
            if (_nLuaFunction != null)
            {
                _nLuaFunction.Dispose();
            }

            if (_nLua != null)
            {
                _nLua.Dispose();
            }

            if (_luaCSharpState != null)
            {
                _luaCSharpState.Dispose();
            }
        }

        _disposed = true;
    }
}
