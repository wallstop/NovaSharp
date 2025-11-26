namespace NovaSharp.Benchmarks
{
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Benchmarks covering script compilation and execution throughput at multiple complexity levels.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires benchmark classes to remain public for reflective discovery."
    )]
    public class ScriptLoadingBenchmarks
    {
        private string _scriptSource = string.Empty;
        private Script _precompiledScript = null!;
        private DynValue _precompiledFunction = DynValue.Nil;

        /// <summary>
        /// Script complexity used for the current benchmark iteration.
        /// </summary>
        [Params(
            ScriptComplexity.Tiny,
            ScriptComplexity.Small,
            ScriptComplexity.Medium,
            ScriptComplexity.Large
        )]
        public ScriptComplexity Complexity { get; set; }

        [GlobalSetup]
        /// <summary>
        /// Prepares the script source and precompiled artifacts before the benchmarks execute.
        /// </summary>
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

        /// <summary>
        /// Compiles and immediately executes the script, exercising end-to-end loading.
        /// </summary>
        [Benchmark(Description = "Compile + Execute")]
        public DynValue CompileAndExecute()
        {
            Script script = new(CoreModules.PresetComplete);
            return script.DoString(_scriptSource, null, $"compile_execute_{Complexity}");
        }

        /// <summary>
        /// Measures script compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile Only")]
        public DynValue CompileOnly()
        {
            Script script = new(CoreModules.PresetComplete);
            return script.LoadString(_scriptSource, null, $"compile_{Complexity}");
        }

        /// <summary>
        /// Executes the precompiled chunk, isolating runtime overhead.
        /// </summary>
        [Benchmark(Description = "Execute Precompiled")]
        public DynValue ExecutePrecompiled() => _precompiledScript.Call(_precompiledFunction);
    }
}
