#if NET8_0_OR_GREATER
using BenchmarkDotNet.Attributes;
using MoonSharp.Interpreter;
using NLua;

namespace MoonSharp.PerformanceComparison;

[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class LuaPerformanceBenchmarks
{
    private string _source = string.Empty;
    private Script _moonSharpScript = null!;
    private DynValue _moonSharpFunction = DynValue.Nil;
    private Lua _nLua = null!;
    private LuaFunction? _nLuaFunction;

    [Params(
        ScriptScenario.TowerOfHanoi,
        ScriptScenario.EightQueens,
        ScriptScenario.CoroutinePingPong
    )]
    public ScriptScenario Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Script.WarmUp();

        _source = BenchmarkScripts.GetScript(Scenario);

        _moonSharpScript = new Script(CoreModules.Preset_Complete);
        _moonSharpFunction = _moonSharpScript.LoadString(_source, null, $"precompiled_{Scenario}");

        _nLua = new Lua();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{Scenario}") as LuaFunction;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _nLuaFunction?.Dispose();
        _nLua.Dispose();
    }

    [Benchmark(Description = "MoonSharp Compile")]
    public DynValue MoonSharpCompile()
    {
        var script = new Script(CoreModules.Preset_Complete);
        return script.LoadString(_source, null, $"compile_{Scenario}");
    }

    [Benchmark(Description = "MoonSharp Execute")]
    public DynValue MoonSharpExecute() => _moonSharpScript.Call(_moonSharpFunction);

    [Benchmark(Description = "NLua Compile")]
    public LuaFunction? NLuaCompile()
    {
        return _nLua.LoadString(_source, $"compile_{Scenario}") as LuaFunction;
    }

    [Benchmark(Description = "NLua Execute")]
    public object? NLuaExecute()
    {
        if (_nLuaFunction == null)
        {
            return null;
        }

        object? result = null;
        foreach (var value in _nLuaFunction.Call())
        {
            result = value;
        }
        return result;
    }
}
#endif
