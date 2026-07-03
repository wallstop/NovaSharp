namespace WallstopStudios.NovaSharp.Comparison;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
using MoonCallbackArguments = MoonSharp.Interpreter.CallbackArguments;
using MoonCoreModules = MoonSharp.Interpreter.CoreModules;
using MoonDynValue = MoonSharp.Interpreter.DynValue;
using MoonScript = MoonSharp.Interpreter.Script;
using MoonScriptExecutionContext = MoonSharp.Interpreter.ScriptExecutionContext;
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
    private Script _novaSharpCachedScript;
    private MoonScript _moonSharpScript;
    private MoonDynValue _moonSharpFunction = MoonDynValue.Nil;
    private MoonScript _moonSharpCachedScript;
    private NLuaState _nLua;
    private NLuaFunction _nLuaFunction;
    private NLuaState _nLuaCached;
    private LuaState _luaCSharpState;
    private LuaClosure _luaCSharpFunction;
    private LuaState _luaCSharpCachedState;
    private string _cachedChunkName = string.Empty;
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
        _cachedChunkName = $"cached_{CurrentScenario}";
        _novaSharpCachedScript = new Script(CoreModulePresets.Complete);
        _novaSharpCachedScript.PrepareString(_source, null, _cachedChunkName);

        _moonSharpScript = new MoonScript(MoonCoreModules.Preset_Complete);
        _moonSharpFunction = _moonSharpScript.LoadString(
            _source,
            null,
            $"precompiled_{CurrentScenario}"
        );
        _moonSharpCachedScript = new MoonScript(MoonCoreModules.Preset_Complete);
        _moonSharpCachedScript.LoadString(_source, null, _cachedChunkName);

        _nLua = new NLuaState();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{CurrentScenario}") as NLuaFunction;
        if (_nLuaFunction == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }
        _nLuaCached = new NLuaState();
        using (NLuaFunction warmCached = LoadNLuaFunction(_nLuaCached, _source, _cachedChunkName))
        {
            GC.KeepAlive(warmCached);
        }

        _luaCSharpState = CreateLuaCSharpState();
        _luaCSharpFunction = _luaCSharpState.Load(
            _source.AsSpan(),
            $"precompiled_{CurrentScenario}",
            _luaCSharpState.Environment
        );
        _luaCSharpCachedState = CreateLuaCSharpState();
        LuaClosure luaCSharpCached = _luaCSharpCachedState.Load(
            _source.AsSpan(),
            _cachedChunkName,
            _luaCSharpCachedState.Environment
        );
        GC.KeepAlive(luaCSharpCached);
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

    [Benchmark(Description = "NovaSharp Cached Compile")]
    /// <summary>
    /// Loads the scenario through a warmed NovaSharp script compilation cache.
    /// </summary>
    public int NovaSharpCachedCompile()
    {
        CompiledScript compiled = _novaSharpCachedScript.PrepareString(
            _source,
            null,
            _cachedChunkName
        );
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

    [Benchmark(Description = "MoonSharp Cached Compile")]
    /// <summary>
    /// Re-loads the scenario through a warmed MoonSharp script instance.
    /// </summary>
    public int MoonSharpCachedCompile()
    {
        MoonDynValue compiled = _moonSharpCachedScript.LoadString(_source, null, _cachedChunkName);
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
        using NLuaFunction compiled = LoadNLuaFunction(
            state,
            _source,
            $"compile_{CurrentScenario}"
        );
        GC.KeepAlive(compiled);
        return 1;
    }

    [Benchmark(Description = "NLua Cached Compile")]
    /// <summary>
    /// Re-loads the scenario through a warmed NLua state.
    /// </summary>
    public int NLuaCachedCompile()
    {
        using NLuaFunction compiled = LoadNLuaFunction(_nLuaCached, _source, _cachedChunkName);
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

    [Benchmark(Description = "LuaCSharp Cached Compile")]
    /// <summary>
    /// Re-loads the scenario through a warmed Lua-CSharp state.
    /// </summary>
    public int LuaCSharpCachedCompile()
    {
        LuaClosure compiled = _luaCSharpCachedState.Load(
            _source.AsSpan(),
            _cachedChunkName,
            _luaCSharpCachedState.Environment
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

    private static NLuaFunction LoadNLuaFunction(NLuaState state, string source, string chunkName)
    {
        NLuaFunction function = state.LoadString(source, chunkName) as NLuaFunction;
        if (function == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }

        return function;
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

            if (_nLuaCached != null)
            {
                _nLuaCached.Dispose();
            }

            if (_luaCSharpState != null)
            {
                _luaCSharpState.Dispose();
            }

            if (_luaCSharpCachedState != null)
            {
                _luaCSharpCachedState.Dispose();
            }
        }

        _disposed = true;
    }
}

/// <summary>
/// BenchmarkDotNet suite that compares Lua-to-CLR and CLR-to-Lua call boundary throughput.
/// </summary>
[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
[SuppressMessage(
    "Usage",
    "CA1515:Consider making public types internal",
    Justification = "BenchmarkDotNet comparison benchmarks must remain public and unsealed."
)]
public class LuaInteropBenchmarks : IDisposable
{
    private const int InteropCallCount = 1_000_000;
    private const string InteropScenarioName = "TwoArgAdd";
    private const double ExpectedLuaToClrTotal = InteropCallCount * (InteropCallCount + 3d) / 2d;
    private const double ExpectedClrToLuaTotal = InteropCallCount * 3d;
    private static readonly string LuaToClrSource = CreateLuaToClrSource(InteropCallCount);
    private const string ClrToLuaSource =
        @"
        function add(a, b)
            return a + b
        end
    ";
    private const string LuaCSharpClrToLuaSource =
        @"
        return function(a, b)
            return a + b
        end
    ";

    private static readonly MethodInfo NLuaAddMethod = GetRequiredMethod(nameof(AddForNLua));

    private Script _novaSharpLuaToClrScript;
    private CompiledScript _novaSharpLuaToClrFunction;
    private Script _novaSharpClrToLuaScript;
    private DynValue _novaSharpAddFunction = DynValue.Nil;
    private DynValue _novaSharpOne = DynValue.Nil;
    private DynValue _novaSharpTwo = DynValue.Nil;
    private MoonScript _moonSharpLuaToClrScript;
    private MoonDynValue _moonSharpLuaToClrFunction = MoonDynValue.Nil;
    private MoonScript _moonSharpClrToLuaScript;
    private MoonDynValue _moonSharpAddFunction = MoonDynValue.Nil;
    private MoonDynValue[] _moonSharpCallArgs = Array.Empty<MoonDynValue>();
    private NLuaState _nLuaLuaToClr;
    private NLuaFunction _nLuaLuaToClrFunction;
    private NLuaState _nLuaClrToLua;
    private NLuaFunction _nLuaAddFunction;
    private object[] _nLuaCallArgs = Array.Empty<object>();
    private LuaState _luaCSharpLuaToClrState;
    private LuaClosure _luaCSharpLuaToClrFunction;
    private LuaState _luaCSharpClrToLuaState;
    private LuaFunction _luaCSharpAddFunction;
    private LuaCSharpValue _luaCSharpOne;
    private LuaCSharpValue _luaCSharpTwo;
    private bool _disposed;

    /// <summary>
    /// Scenario executed by the interop comparison benchmarks.
    /// </summary>
    [Params(InteropScenarioName)]
    public string ScenarioName { get; set; } = InteropScenarioName;

    [GlobalSetup]
    /// <summary>
    /// Registers equivalent two-argument add functions and cached Lua closures for each engine.
    /// </summary>
    public void Setup()
    {
        Script.WarmUp();
        MoonScript.WarmUp();

        _novaSharpLuaToClrScript = new Script(CoreModulePresets.Complete);
        _novaSharpLuaToClrScript.Globals["add"] = DynValue.NewCallbackView(
            (ScriptFunctionCallbackViewNoContext)AddForNovaSharp,
            "add"
        );
        _novaSharpLuaToClrFunction = _novaSharpLuaToClrScript.PrepareString(
            LuaToClrSource,
            null,
            "interop_lua_to_clr"
        );
        _novaSharpClrToLuaScript = new Script(CoreModulePresets.Complete);
        _novaSharpClrToLuaScript.DoString(ClrToLuaSource, null, "interop_clr_to_lua");
        _novaSharpAddFunction = _novaSharpClrToLuaScript.Globals.Get("add");
        _novaSharpOne = DynValue.FromNumber(1);
        _novaSharpTwo = DynValue.FromNumber(2);

        _moonSharpLuaToClrScript = new MoonScript(MoonCoreModules.Preset_Complete);
        _moonSharpLuaToClrScript.Globals["add"] = MoonDynValue.NewCallback(AddForMoonSharp, "add");
        _moonSharpLuaToClrFunction = _moonSharpLuaToClrScript.LoadString(
            LuaToClrSource,
            null,
            "interop_lua_to_clr"
        );
        _moonSharpClrToLuaScript = new MoonScript(MoonCoreModules.Preset_Complete);
        _moonSharpClrToLuaScript.DoString(ClrToLuaSource, null, "interop_clr_to_lua");
        _moonSharpAddFunction = _moonSharpClrToLuaScript.Globals.Get("add");
        _moonSharpCallArgs = new[] { MoonDynValue.NewNumber(1), MoonDynValue.NewNumber(2) };

        _nLuaLuaToClr = new NLuaState();
        _nLuaLuaToClr.RegisterFunction("add", null, NLuaAddMethod);
        _nLuaLuaToClrFunction = LoadNLuaFunction(
            _nLuaLuaToClr,
            LuaToClrSource,
            "interop_lua_to_clr"
        );
        _nLuaClrToLua = new NLuaState();
        _nLuaClrToLua.DoString(ClrToLuaSource, "interop_clr_to_lua");
        _nLuaAddFunction = GetRequiredNLuaFunction(_nLuaClrToLua, "add");
        _nLuaCallArgs = new object[] { 1d, 2d };

        _luaCSharpLuaToClrState = CreateLuaCSharpState();
        _luaCSharpLuaToClrState.Environment["add"] = new LuaFunction(AddForLuaCSharp);
        _luaCSharpLuaToClrFunction = _luaCSharpLuaToClrState.Load(
            LuaToClrSource.AsSpan(),
            "interop_lua_to_clr",
            _luaCSharpLuaToClrState.Environment
        );
        _luaCSharpClrToLuaState = CreateLuaCSharpState();
        LuaClosure luaCSharpChunk = _luaCSharpClrToLuaState.Load(
            LuaCSharpClrToLuaSource.AsSpan(),
            "interop_clr_to_lua",
            _luaCSharpClrToLuaState.Environment
        );
        _luaCSharpAddFunction = RunLuaCSharp(_luaCSharpClrToLuaState, luaCSharpChunk)
            .Read<LuaFunction>();
        _luaCSharpOne = 1d;
        _luaCSharpTwo = 2d;
    }

    [GlobalCleanup]
    /// <summary>
    /// Releases runtime resources after the benchmark run.
    /// </summary>
    public void Cleanup() => Dispose();

    [Benchmark(Description = "NovaSharp LuaToClrInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million Lua-to-CLR two-argument callback calls on NovaSharp.
    /// </summary>
    public DynValue NovaSharpLuaToClrInterop()
    {
        DynValue result = _novaSharpLuaToClrFunction.Execute();
        ValidateInteropTotal(
            nameof(NovaSharpLuaToClrInterop),
            result.Number,
            ExpectedLuaToClrTotal
        );
        return result;
    }

    [Benchmark(Description = "MoonSharp LuaToClrInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million Lua-to-CLR two-argument callback calls on MoonSharp.
    /// </summary>
    public MoonDynValue MoonSharpLuaToClrInterop()
    {
        MoonDynValue result = _moonSharpLuaToClrScript.Call(_moonSharpLuaToClrFunction);
        ValidateInteropTotal(
            nameof(MoonSharpLuaToClrInterop),
            result.Number,
            ExpectedLuaToClrTotal
        );
        return result;
    }

    [Benchmark(Description = "NLua LuaToClrInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million Lua-to-CLR two-argument callback calls on NLua.
    /// </summary>
    public object NLuaLuaToClrInterop()
    {
        object result = ReadFirstNLuaResult(
            nameof(NLuaLuaToClrInterop),
            _nLuaLuaToClrFunction.Call()
        );

        ValidateInteropTotal(
            nameof(NLuaLuaToClrInterop),
            ReadNLuaNumber(result),
            ExpectedLuaToClrTotal
        );
        return result;
    }

    [Benchmark(Description = "LuaCSharp LuaToClrInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million Lua-to-CLR two-argument callback calls on Lua-CSharp.
    /// </summary>
    public LuaCSharpValue LuaCSharpLuaToClrInterop()
    {
        LuaCSharpValue result = RunLuaCSharp(_luaCSharpLuaToClrState, _luaCSharpLuaToClrFunction);
        ValidateInteropTotal(
            nameof(LuaCSharpLuaToClrInterop),
            result.Read<double>(),
            ExpectedLuaToClrTotal
        );
        return result;
    }

    [Benchmark(Description = "NovaSharp ClrToLuaInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million CLR-to-Lua two-argument function calls on NovaSharp.
    /// </summary>
    public double NovaSharpClrToLuaInterop()
    {
        double total = 0;
        for (int i = 0; i < InteropCallCount; i++)
        {
            total += _novaSharpClrToLuaScript
                .Call(_novaSharpAddFunction, _novaSharpOne, _novaSharpTwo)
                .Number;
        }

        ValidateInteropTotal(nameof(NovaSharpClrToLuaInterop), total, ExpectedClrToLuaTotal);
        return total;
    }

    [Benchmark(Description = "MoonSharp ClrToLuaInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million CLR-to-Lua two-argument function calls on MoonSharp.
    /// </summary>
    public double MoonSharpClrToLuaInterop()
    {
        double total = 0;
        for (int i = 0; i < InteropCallCount; i++)
        {
            total += _moonSharpClrToLuaScript
                .Call(_moonSharpAddFunction, _moonSharpCallArgs)
                .Number;
        }

        ValidateInteropTotal(nameof(MoonSharpClrToLuaInterop), total, ExpectedClrToLuaTotal);
        return total;
    }

    [Benchmark(Description = "NLua ClrToLuaInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million CLR-to-Lua two-argument function calls on NLua.
    /// </summary>
    public double NLuaClrToLuaInterop()
    {
        double total = 0;
        for (int i = 0; i < InteropCallCount; i++)
        {
            total += ReadNLuaNumber(
                ReadFirstNLuaResult(
                    nameof(NLuaClrToLuaInterop),
                    _nLuaAddFunction.Call(_nLuaCallArgs)
                )
            );
        }

        ValidateInteropTotal(nameof(NLuaClrToLuaInterop), total, ExpectedClrToLuaTotal);
        return total;
    }

    [Benchmark(Description = "LuaCSharp ClrToLuaInterop", OperationsPerInvoke = InteropCallCount)]
    /// <summary>
    /// Executes one million CLR-to-Lua two-argument function calls on Lua-CSharp.
    /// </summary>
    public double LuaCSharpClrToLuaInterop()
    {
        double total = 0;
        for (int i = 0; i < InteropCallCount; i++)
        {
            total += CallLuaCSharpFunction(
                    _luaCSharpClrToLuaState,
                    _luaCSharpAddFunction,
                    _luaCSharpOne,
                    _luaCSharpTwo
                )
                .Read<double>();
        }

        ValidateInteropTotal(nameof(LuaCSharpClrToLuaInterop), total, ExpectedClrToLuaTotal);
        return total;
    }

    private static DynValue AddForNovaSharp(CallbackArgumentsView args)
    {
        return DynValue.FromNumber(args[0].Number + args[1].Number);
    }

    private static string CreateLuaToClrSource(int callCount) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $@"
        local total = 0
        for i = 1, {callCount} do
            total = total + add(i, 1)
        end
        return total
    "
        );

    private static MoonDynValue AddForMoonSharp(
        MoonScriptExecutionContext context,
        MoonCallbackArguments args
    )
    {
        return MoonDynValue.NewNumber(args[0].Number + args[1].Number);
    }

    private static double AddForNLua(double left, double right)
    {
        return left + right;
    }

    private static ValueTask<int> AddForLuaCSharp(
        LuaFunctionExecutionContext context,
        CancellationToken cancellationToken
    )
    {
        double left = context.GetArgument<double>(0);
        double right = context.GetArgument<double>(1);
        return new ValueTask<int>(context.Return(left + right));
    }

    private static NLuaFunction LoadNLuaFunction(NLuaState state, string source, string chunkName)
    {
        NLuaFunction function = state.LoadString(source, chunkName) as NLuaFunction;
        if (function == null)
        {
            throw new InvalidOperationException("NLua failed to compile the benchmark script.");
        }

        return function;
    }

    private static NLuaFunction GetRequiredNLuaFunction(NLuaState state, string functionName)
    {
        NLuaFunction function = state.GetFunction(functionName);
        if (function == null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "NLua function '{0}' was not found after benchmark setup.",
                    functionName
                )
            );
        }

        return function;
    }

    private static LuaState CreateLuaCSharpState()
    {
        LuaState state = LuaState.Create();
        state.OpenStandardLibraries();
        return state;
    }

    private static LuaCSharpValue RunLuaCSharp(LuaState state, LuaFunction function)
    {
        int returnCount = GetLuaCSharpReturnCount(state.RunAsync(function, CancellationToken.None));
        if (returnCount <= 0)
        {
            return LuaCSharpValue.Nil;
        }

        using LuaStackReader reader = state.ReadStack(returnCount);
        ReadOnlySpan<LuaCSharpValue> values = reader.AsSpan();
        return values.Length > 0 ? values[^1] : LuaCSharpValue.Nil;
    }

    private static LuaCSharpValue CallLuaCSharpFunction(
        LuaState state,
        LuaFunction function,
        LuaCSharpValue arg1,
        LuaCSharpValue arg2
    )
    {
        int basePosition = state.Stack.Count;
        state.Stack.Push(function);
        state.Stack.Push(arg1);
        state.Stack.Push(arg2);
        int returnCount = GetLuaCSharpReturnCount(
            state.CallAsync(basePosition, basePosition, CancellationToken.None)
        );
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

    private static double ReadNLuaNumber(object value)
    {
        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is long longValue)
        {
            return longValue;
        }

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    private static object ReadFirstNLuaResult(string methodName, object[] values)
    {
        if (values.Length > 0)
        {
            return values[0];
        }

        throw new InvalidOperationException(
            string.Format(CultureInfo.InvariantCulture, "{0} returned no values.", methodName)
        );
    }

    private static void ValidateInteropTotal(string methodName, double actual, double expected)
    {
        if (actual == expected)
        {
            return;
        }

        throw new InvalidOperationException(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} returned {1:R}; expected {2:R}.",
                methodName,
                actual,
                expected
            )
        );
    }

    private static MethodInfo GetRequiredMethod(string name)
    {
        MethodInfo method = typeof(LuaInteropBenchmarks).GetMethod(
            name,
            BindingFlags.Static | BindingFlags.NonPublic
        );
        if (method == null)
        {
            throw new MissingMethodException(typeof(LuaInteropBenchmarks).FullName, name);
        }

        return method;
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
            if (_nLuaLuaToClrFunction != null)
            {
                _nLuaLuaToClrFunction.Dispose();
            }

            if (_nLuaAddFunction != null)
            {
                _nLuaAddFunction.Dispose();
            }

            if (_nLuaLuaToClr != null)
            {
                _nLuaLuaToClr.Dispose();
            }

            if (_nLuaClrToLua != null)
            {
                _nLuaClrToLua.Dispose();
            }

            if (_luaCSharpLuaToClrState != null)
            {
                _luaCSharpLuaToClrState.Dispose();
            }

            if (_luaCSharpClrToLuaState != null)
            {
                _luaCSharpClrToLuaState.Dispose();
            }
        }

        _disposed = true;
    }
}
