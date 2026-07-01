namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using BenchmarkDotNet.Attributes;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
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
        private byte[] _scriptSourceBytes = Array.Empty<byte>();
        private string _cachedFriendlyName = string.Empty;
        private Script _precompiledScript;
        private Script _precompiledStreamScript;
        private Script _precompiledFileScript;
        private Script _cachedScript;
        private Script _namedCachedScript;
        private Script _cachedFileScript;
        private Script _namedCachedFileScript;
        private string _cachedFileName = string.Empty;
        private string _cachedFileFriendlyName = string.Empty;
        private DynValue _precompiledFunction = DynValue.Nil;
        private CompiledScript _compiledHandle;
        private CompiledScript _compiledStreamHandle;
        private CompiledScript _compiledFileHandle;
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
            _scriptSourceBytes = System.Text.Encoding.UTF8.GetBytes(_scriptSource);
            _precompiledScript = new Script(CoreModulePresets.Complete);
            _compiledHandle = _precompiledScript.PrepareString(
                _scriptSource,
                null,
                $"precompiled_{complexity}"
            );
            _precompiledFunction = _compiledHandle.Function;

            _precompiledStreamScript = new Script(CoreModulePresets.Complete);
            using (MemoryStream stream = new(_scriptSourceBytes))
            {
                _compiledStreamHandle = _precompiledStreamScript.PrepareStream(
                    stream,
                    codeFriendlyName: $"precompiled_stream_{complexity}"
                );
            }

            _precompiledFileScript = new Script(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = new StaticStringScriptLoader(_scriptSource) }
            );
            _compiledFileHandle = _precompiledFileScript.PrepareFile(
                $"precompiled_file_{complexity}.lua"
            );

            _cachedScript = new Script(CoreModulePresets.Complete);
            _cachedScript.LoadString(_scriptSource);

            _cachedFriendlyName = $"cached_{complexity}";
            _namedCachedScript = new Script(CoreModulePresets.Complete);
            _namedCachedScript.LoadString(_scriptSource, null, _cachedFriendlyName);

            _cachedFileName = $"cached_file_{complexity}.lua";
            _cachedFileScript = new Script(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    EnableScriptCaching = true,
                    ScriptLoader = new StaticStringScriptLoader(_scriptSource),
                }
            );
            _cachedFileScript.LoadFile(_cachedFileName);

            _cachedFileFriendlyName = $"cached_file_friendly_{complexity}.lua";
            _namedCachedFileScript = new Script(
                CoreModulePresets.Complete,
                new ScriptOptions
                {
                    EnableScriptCaching = true,
                    ScriptLoader = new StaticStringScriptLoader(_scriptSource),
                }
            );
            _namedCachedFileScript.LoadFile(
                _cachedFileName,
                friendlyFilename: _cachedFileFriendlyName
            );
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
        /// Measures stream compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile Stream Only")]
        public DynValue CompileStreamOnly()
        {
            Script script = new(CoreModulePresets.Complete);
            using MemoryStream stream = new(_scriptSourceBytes);
            return script
                .CompileStream(stream, codeFriendlyName: $"compile_stream_{_currentComplexity}")
                .Function;
        }

        /// <summary>
        /// Measures file-loader compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile File Only")]
        public DynValue CompileFileOnly()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = new StaticStringScriptLoader(_scriptSource) }
            );
            return script.CompileFile($"compile_file_{_currentComplexity}.lua").Function;
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
        /// Executes a file-backed chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoFile Cached")]
        public DynValue DoFileCached() => _cachedFileScript.DoFile(_cachedFileName);

        /// <summary>
        /// Executes a named file-backed chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoFile Cached Named")]
        public DynValue DoFileCachedNamed() =>
            _namedCachedFileScript.DoFile(
                _cachedFileName,
                codeFriendlyName: _cachedFileFriendlyName
            );

        /// <summary>
        /// Executes the precompiled chunk, isolating runtime overhead.
        /// </summary>
        [Benchmark(Description = "Execute Precompiled")]
        public DynValue ExecutePrecompiled() => _precompiledScript.Call(_precompiledFunction);

        /// <summary>
        /// Executes the explicit prepare-once handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Prepared String Handle")]
        public DynValue ExecutePreparedStringHandle() => _compiledHandle.Execute();

        /// <summary>
        /// Executes a prepared handle and converts the first scalar result through ExecuteAs.
        /// </summary>
        [Benchmark(Description = "Execute Prepared String Handle As Double")]
        public double ExecutePreparedStringHandleAsDouble() => _compiledHandle.ExecuteAs<double>();

        /// <summary>
        /// Executes a prepared handle and reads the first scalar result through the strict number helper.
        /// </summary>
        [Benchmark(Description = "Execute Prepared String Handle Number")]
        public double ExecutePreparedStringHandleNumber() => _compiledHandle.ExecuteNumber();

        /// <summary>
        /// Executes a stream-prepared handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Stream Handle")]
        public DynValue ExecutePreparedStreamHandle() => _compiledStreamHandle.Execute();

        /// <summary>
        /// Executes a file-prepared handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Prepared File Handle")]
        public DynValue ExecutePreparedFileHandle() => _compiledFileHandle.Execute();

        private sealed class StaticStringScriptLoader : ScriptLoaderBase
        {
            private readonly string _source;

            public StaticStringScriptLoader(string source)
            {
                _source = source;
            }

            /// <inheritdoc />
            public override object LoadFile(string file, Table globalContext)
            {
                return _source;
            }

            /// <inheritdoc />
            public override bool ScriptFileExists(string name)
            {
                return true;
            }
        }
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
        private DynValue _nestedFunction = DynValue.Nil;
        private DynValue _sixArgFunction = DynValue.Nil;
        private DynValue _sevenArgFunction = DynValue.Nil;
        private DynValue _zeroArgFunction = DynValue.Nil;
        private object[] _nestedFunctionPath = Array.Empty<object>();
        private object[] _paddedNestedFunctionPath = Array.Empty<object>();
        private DynValue _arg1 = DynValue.Nil;
        private DynValue _arg2 = DynValue.Nil;
        private DynValue _arg3 = DynValue.Nil;
        private DynValue _arg4 = DynValue.Nil;
        private DynValue _arg5 = DynValue.Nil;
        private DynValue _arg6 = DynValue.Nil;
        private DynValue _arg7 = DynValue.Nil;
        private CompiledScript _boundGlobalHandle;
        private CompiledScript _boundNestedGlobalHandle;
        private CompiledScript _boundSixArgHandle;
        private CompiledScript _boundSevenArgHandle;
        private CompiledScript _boundZeroArgHandle;

        /// <summary>
        /// Prepares a global Lua function and cached argument values.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _script = new Script(CoreModulePresets.Complete);
            _script.DoString(
                "function update(a, b, c) return a + b + c end; "
                    + "function update6(a, b, c, d, e, f) return f end; "
                    + "function update7(a, b, c, d, e, f, g) return g end; "
                    + "function tick() return 42 end; "
                    + "api = { system = { update = function(a, b, c) return a + b + c end } }"
            );
            _function = _script.Globals.Get("update");
            _nestedFunction = _script.Globals.Get("api", "system", "update");
            _sixArgFunction = _script.Globals.Get("update6");
            _sevenArgFunction = _script.Globals.Get("update7");
            _zeroArgFunction = _script.Globals.Get("tick");
            _nestedFunctionPath = new object[] { "api", "system", "update" };
            _paddedNestedFunctionPath = new object[]
            {
                "ignored",
                "api",
                "system",
                "update",
                "ignored",
            };
            _boundGlobalHandle = _script.PrepareGlobalFunction("update");
            _boundNestedGlobalHandle = _script.PrepareGlobalFunction("api", "system", "update");
            _boundSixArgHandle = _script.PrepareGlobalFunction("update6");
            _boundSevenArgHandle = _script.PrepareGlobalFunction("update7");
            _boundZeroArgHandle = _script.PrepareGlobalFunction("tick");
            _arg1 = DynValue.FromNumber(1);
            _arg2 = DynValue.FromNumber(2);
            _arg3 = DynValue.FromNumber(3);
            _arg4 = DynValue.FromNumber(4);
            _arg5 = DynValue.FromNumber(5);
            _arg6 = DynValue.FromNumber(6);
            _arg7 = DynValue.FromNumber(7);
        }

        /// <summary>
        /// Resolves a global function on every call before invoking it.
        /// </summary>
        [Benchmark(Description = "Call Global Lookup")]
        public DynValue CallGlobalLookup() =>
            _script.Call(_script.Globals.Get("update"), _arg1, _arg2, _arg3);

        /// <summary>
        /// Resolves a nested global function on every call before invoking it.
        /// </summary>
        [Benchmark(Description = "Call Nested Global Lookup")]
        public DynValue CallNestedGlobalLookup() =>
            _script.Call(_script.Globals.Get("api", "system", "update"), _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Global")]
        public DynValue CallCachedGlobal() => _script.Call(_function, _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached nested global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Nested Global")]
        public DynValue CallCachedNestedGlobal() =>
            _script.Call(_nestedFunction, _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached six-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached 6-Arg Global")]
        public DynValue CallCachedSixArgGlobal() =>
            _script.Call(_sixArgFunction, _arg1, _arg2, _arg3, _arg4, _arg5, _arg6);

        /// <summary>
        /// Calls a manually cached seven-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached 7-Arg Global")]
        public DynValue CallCachedSevenArgGlobal() =>
            _script.Call(_sevenArgFunction, _arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);

        /// <summary>
        /// Calls a manually cached zero-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Zero-Arg Global")]
        public DynValue CallCachedZeroArgGlobal() => _script.Call(_zeroArgFunction);

        /// <summary>
        /// Executes a global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Global Handle")]
        public DynValue ExecutePreparedGlobalHandle() =>
            _boundGlobalHandle.Execute(_arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a global function handle and converts the first scalar result through ExecuteAs.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Global Handle As Double")]
        public double ExecutePreparedGlobalHandleAsDouble() =>
            _boundGlobalHandle.ExecuteAs<double>(_arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a global function handle and reads the first scalar result through the strict number helper.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Global Handle Number")]
        public double ExecutePreparedGlobalHandleNumber() =>
            _boundGlobalHandle.ExecuteNumber(_arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a nested global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Nested Global Handle")]
        public DynValue ExecutePreparedNestedGlobalHandle() =>
            _boundNestedGlobalHandle.Execute(_arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a six-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared 6-Arg Handle")]
        public DynValue ExecutePreparedSixArgHandle() =>
            _boundSixArgHandle.Execute(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6);

        /// <summary>
        /// Executes a seven-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared 7-Arg Handle")]
        public DynValue ExecutePreparedSevenArgHandle() =>
            _boundSevenArgHandle.Execute(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);

        /// <summary>
        /// Executes a zero-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Zero-Arg Handle")]
        public DynValue ExecutePreparedZeroArgHandle() => _boundZeroArgHandle.Execute();

        /// <summary>
        /// Resolves a top-level global function through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Prepare Global Handle")]
        public CompiledScript PrepareGlobalHandle() => _script.PrepareGlobalFunction("update");

        /// <summary>
        /// Resolves a nested global function through the fixed-key public prepare API.
        /// </summary>
        [Benchmark(Description = "Prepare Nested Global Fixed Handle")]
        public CompiledScript PrepareNestedGlobalFixedHandle() =>
            _script.PrepareGlobalFunction("api", "system", "update");

        /// <summary>
        /// Resolves a nested global function through the caller-owned array path prepare API.
        /// </summary>
        [Benchmark(Description = "Prepare Nested Global Array Path Handle")]
        public CompiledScript PrepareNestedGlobalArrayPathHandle() =>
            _script.PrepareGlobalFunctionPath(_nestedFunctionPath);

        /// <summary>
        /// Resolves a nested global function through the caller-owned span path prepare API.
        /// </summary>
        [Benchmark(Description = "Prepare Nested Global Span Path Handle")]
        public CompiledScript PrepareNestedGlobalSpanPathHandle() =>
            _script.PrepareGlobalFunctionPath(_nestedFunctionPath.AsSpan());

        /// <summary>
        /// Resolves a nested global function through a caller-owned path slice.
        /// </summary>
        [Benchmark(Description = "Prepare Nested Global Span Slice Path Handle")]
        public CompiledScript PrepareNestedGlobalSpanSlicePathHandle() =>
            _script.PrepareGlobalFunctionPath(_paddedNestedFunctionPath.AsSpan(1, 3));
    }
}
