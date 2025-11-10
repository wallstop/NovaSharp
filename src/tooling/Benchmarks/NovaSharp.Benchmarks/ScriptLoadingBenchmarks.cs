namespace NovaSharp.Benchmarks
{
    using BenchmarkDotNet.Attributes;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;

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
            _precompiledScript = new Script(CoreModules.PresetComplete);
            _precompiledFunction = _precompiledScript.LoadString(
                _scriptSource,
                null,
                $"precompiled_{Complexity}"
            );
        }

        [Benchmark(Description = "Compile + Execute")]
        public DynValue CompileAndExecute()
        {
            Script script = new(CoreModules.PresetComplete);
            return script.DoString(_scriptSource, null, $"compile_execute_{Complexity}");
        }

        [Benchmark(Description = "Compile Only")]
        public DynValue CompileOnly()
        {
            Script script = new(CoreModules.PresetComplete);
            return script.LoadString(_scriptSource, null, $"compile_{Complexity}");
        }

        [Benchmark(Description = "Execute Precompiled")]
        public DynValue ExecutePrecompiled() => _precompiledScript.Call(_precompiledFunction);
    }
}
