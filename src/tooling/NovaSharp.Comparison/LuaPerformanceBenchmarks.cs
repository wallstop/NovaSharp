namespace NovaSharp.Comparison;

using BenchmarkDotNet.Attributes;
using Interpreter;
using NLua;

[MemoryDiagnoser]
[HideColumns("Job", "Error", "StdDev")]
public class LuaPerformanceBenchmarks
{
    private string _source = string.Empty;
    private Script _novaSharpScript = null!;
    private DynValue _novaSharpFunction = DynValue.Nil;
    private Lua _nLua = null!;
    private LuaFunction _nLuaFunction;

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

        _novaSharpScript = new Script(CoreModules.PresetComplete);
        _novaSharpFunction = _novaSharpScript.LoadString(_source, null, $"precompiled_{Scenario}");

        _nLua = new Lua();
        _nLuaFunction = _nLua.LoadString(_source, $"precompiled_{Scenario}") as LuaFunction;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _nLuaFunction?.Dispose();
        _nLua.Dispose();
    }

    [Benchmark(Description = "NovaSharp Compile")]
    public DynValue NovaSharpCompile()
    {
        Script script = new(CoreModules.PresetComplete);
        return script.LoadString(_source, null, $"compile_{Scenario}");
    }

    [Benchmark(Description = "NovaSharp Execute")]
    public DynValue NovaSharpExecute() => _novaSharpScript.Call(_novaSharpFunction);

    [Benchmark(Description = "NLua Compile")]
    public LuaFunction NLuaCompile()
    {
        return _nLua.LoadString(_source, $"compile_{Scenario}") as LuaFunction;
    }

    [Benchmark(Description = "NLua Execute")]
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
}
