using BenchmarkDotNet.Attributes;
using MoonSharp.Interpreter;

namespace MoonSharp.Benchmarks;

[MemoryDiagnoser]
public class ScriptLoadingBenchmarks
{
    private string _scriptSource = string.Empty;
    private Script _precompiledScript = null!;
    private DynValue _precompiledFunction = DynValue.Nil;

    [Params(
        ScriptComplexity.Tiny,
        ScriptComplexity.Small,
        ScriptComplexity.Medium,
        ScriptComplexity.Large
    )]
    public ScriptComplexity Complexity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _scriptSource = LuaScriptCorpus.GetCompilationScript(Complexity);
        _precompiledScript = new Script(CoreModules.Preset_Complete);
        _precompiledFunction = _precompiledScript.LoadString(
            _scriptSource,
            null,
            $"precompiled_{Complexity}"
        );
    }

    [Benchmark(Description = "Compile + Execute")]
    public DynValue CompileAndExecute()
    {
        var script = new Script(CoreModules.Preset_Complete);
        return script.DoString(_scriptSource, null, $"compile_execute_{Complexity}");
    }

    [Benchmark(Description = "Compile Only")]
    public DynValue CompileOnly()
    {
        var script = new Script(CoreModules.Preset_Complete);
        return script.LoadString(_scriptSource, null, $"compile_{Complexity}");
    }

    [Benchmark(Description = "Execute Precompiled")]
    public DynValue ExecutePrecompiled() => _precompiledScript.Call(_precompiledFunction);
}
