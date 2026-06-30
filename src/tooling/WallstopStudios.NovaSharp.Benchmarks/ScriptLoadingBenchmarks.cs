namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Benchmarks covering script compilation and execution throughput at multiple complexity levels.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class ScriptLoadingBenchmarks
    {
        private string _scriptSource = string.Empty;
        private string _cachedFriendlyName = string.Empty;
        private Script _precompiledScript;
        private Script _cachedScript;
        private Script _namedCachedScript;
        private DynValue _precompiledFunction = DynValue.Nil;
        private CompiledScript _compiledHandle;
        private ScriptComplexity _currentComplexity;

        /// <summary>
        /// Script complexity used for the current benchmark iteration.
        /// </summary>
        [Params(
            nameof(ScriptComplexity.Tiny),
            nameof(ScriptComplexity.Small),
            nameof(ScriptComplexity.Medium),
            nameof(ScriptComplexity.Large)
        )]
        public string ComplexityName { get; set; } = nameof(ScriptComplexity.Tiny);

        private ScriptComplexity CurrentComplexity
        {
            get
            {
                ArgumentException.ThrowIfNullOrEmpty(ComplexityName);
                return Enum.Parse<ScriptComplexity>(ComplexityName, ignoreCase: false);
            }
        }

        [GlobalSetup]
        /// <summary>
        /// Prepares the script source and precompiled artifacts before the benchmarks execute.
        /// </summary>
        public void Setup()
        {
            ScriptComplexity complexity = CurrentComplexity;
            _currentComplexity = complexity;

            _scriptSource = LuaScriptCorpus.GetCompilationScript(complexity);
            _precompiledScript = new Script(CoreModulePresets.Complete);
            _compiledHandle = _precompiledScript.CompileString(
                _scriptSource,
                null,
                $"precompiled_{complexity}"
            );
            _precompiledFunction = _compiledHandle.Function;

            _cachedScript = new Script(CoreModulePresets.Complete);
            _cachedScript.LoadString(_scriptSource);

            _cachedFriendlyName = $"cached_{complexity}";
            _namedCachedScript = new Script(CoreModulePresets.Complete);
            _namedCachedScript.LoadString(_scriptSource, null, _cachedFriendlyName);
        }

        /// <summary>
        /// Compiles and immediately executes the script, exercising end-to-end loading.
        /// </summary>
        [Benchmark(Description = "Compile + Execute")]
        public DynValue CompileAndExecute()
        {
            Script script = new(CoreModulePresets.Complete);
            return script.DoString(_scriptSource, null, $"compile_execute_{_currentComplexity}");
        }

        /// <summary>
        /// Measures script compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile Only")]
        public DynValue CompileOnly()
        {
            Script script = new(CoreModulePresets.Complete);
            return script.LoadString(_scriptSource, null, $"compile_{_currentComplexity}");
        }

        /// <summary>
        /// Loads a chunk already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Load Cached")]
        public DynValue LoadCached() => _cachedScript.LoadString(_scriptSource);

        /// <summary>
        /// Loads a named chunk already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Load Cached Named")]
        public DynValue LoadCachedNamed() =>
            _namedCachedScript.LoadString(_scriptSource, null, _cachedFriendlyName);

        /// <summary>
        /// Executes a chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoString Cached")]
        public DynValue DoStringCached() => _cachedScript.DoString(_scriptSource);

        /// <summary>
        /// Executes a named chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoString Cached Named")]
        public DynValue DoStringCachedNamed() =>
            _namedCachedScript.DoString(_scriptSource, null, _cachedFriendlyName);

        /// <summary>
        /// Executes the precompiled chunk, isolating runtime overhead.
        /// </summary>
        [Benchmark(Description = "Execute Precompiled")]
        public DynValue ExecutePrecompiled() => _precompiledScript.Call(_precompiledFunction);

        /// <summary>
        /// Executes the explicit compile-once handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Compiled Handle")]
        public DynValue ExecuteCompiledHandle() => _compiledHandle.Execute();
    }

    /// <summary>
    /// Benchmarks repeated global function invocation patterns.
    /// </summary>
    [MemoryDiagnoser]
    [SuppressMessage(
        "Usage",
        "CA1515:Consider making public types internal",
        Justification = "BenchmarkDotNet requires public, non-sealed benchmark classes."
    )]
    public class BoundFunctionBenchmarks
    {
        private Script _script;
        private DynValue _function = DynValue.Nil;
        private DynValue _arg1 = DynValue.Nil;
        private DynValue _arg2 = DynValue.Nil;
        private DynValue _arg3 = DynValue.Nil;
        private CompiledScript _boundGlobalHandle;

        /// <summary>
        /// Prepares a global Lua function and cached argument values.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _script.DoString("function update(a, b, c) return a + b + c end");
            _function = _script.Globals.Get("update");
            _boundGlobalHandle = _script.BindGlobalFunction("update");
            _arg1 = DynValue.FromNumber(1);
            _arg2 = DynValue.FromNumber(2);
            _arg3 = DynValue.FromNumber(3);
        }

        /// <summary>
        /// Resolves a global function on every call before invoking it.
        /// </summary>
        [Benchmark(Description = "Call Global Lookup")]
        public DynValue CallGlobalLookup() =>
            _script.Call(_script.Globals.Get("update"), _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Global")]
        public DynValue CallCachedGlobal() => _script.Call(_function, _arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a global function handle resolved once through the public binding API.
        /// </summary>
        [Benchmark(Description = "Execute Bound Global Handle")]
        public DynValue ExecuteBoundGlobalHandle() =>
            _boundGlobalHandle.Execute(_arg1, _arg2, _arg3);
    }
}
