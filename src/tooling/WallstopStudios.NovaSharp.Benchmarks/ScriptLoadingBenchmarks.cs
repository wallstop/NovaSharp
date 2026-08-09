namespace WallstopStudios.NovaSharp.Benchmarks
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using global::NovaSharp;
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
        private string _functionSource = string.Empty;
        private byte[] _scriptSourceBytes = Array.Empty<byte>();
        private string _cachedFriendlyName = string.Empty;
        private string _cachedFunctionFriendlyName = string.Empty;
        private Script _precompiledScript;
        private Script _precompiledStreamScript;
        private Script _precompiledFileScript;
        private Script _cachedScript;
        private Script _namedCachedScript;
        private Script _cachedFunctionScript;
        private Script _namedCachedFunctionScript;
        private Script _cachedFileScript;
        private Script _namedCachedFileScript;
        private string _cachedFileName = string.Empty;
        private string _cachedFileFriendlyName = string.Empty;
        private LuaValue _precompiledFunction = LuaValue.Nil;
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
            _functionSource = string.Concat("function()\n", _scriptSource, "\nend");
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

            _cachedFunctionScript = new Script(CoreModulePresets.Complete);
            _cachedFunctionScript.PrepareFunction(_functionSource);

            _cachedFunctionFriendlyName = $"cached_function_{complexity}";
            _namedCachedFunctionScript = new Script(CoreModulePresets.Complete);
            _namedCachedFunctionScript.PrepareFunction(
                _functionSource,
                funcFriendlyName: _cachedFunctionFriendlyName
            );

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
        public LuaValue CompileAndExecute()
        {
            Script script = new(CoreModulePresets.Complete);
            return script.DoString(_scriptSource, null, $"compile_execute_{_currentComplexity}");
        }

        /// <summary>
        /// Measures script compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile Only")]
        public LuaValue CompileOnly()
        {
            Script script = new(CoreModulePresets.Complete);
            return script.LoadString(_scriptSource, null, $"compile_{_currentComplexity}");
        }

        /// <summary>
        /// Measures stream compilation without executing the resulting chunk.
        /// </summary>
        [Benchmark(Description = "Compile Stream Only")]
        public LuaValue CompileStreamOnly()
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
        public LuaValue CompileFileOnly()
        {
            Script script = new(
                CoreModulePresets.Complete,
                new ScriptOptions { ScriptLoader = new StaticStringScriptLoader(_scriptSource) }
            );
            return script.CompileFile($"compile_file_{_currentComplexity}.lua").Function;
        }

        /// <summary>
        /// Measures standalone function preparation without executing the resulting function.
        /// </summary>
        [Benchmark(Description = "Prepare Function Only")]
        public LuaValue PrepareFunctionOnly()
        {
            Script script = new(CoreModulePresets.Complete);
            return script
                .PrepareFunction(
                    _functionSource,
                    funcFriendlyName: $"prepare_function_{_currentComplexity}"
                )
                .Function;
        }

        /// <summary>
        /// Loads a chunk already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Load Cached")]
        public LuaValue LoadCached() => _cachedScript.LoadString(_scriptSource);

        /// <summary>
        /// Loads a named chunk already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Load Cached Named")]
        public LuaValue LoadCachedNamed() =>
            _namedCachedScript.LoadString(_scriptSource, null, _cachedFriendlyName);

        /// <summary>
        /// Prepares a standalone function already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Prepare Function Cached")]
        public LuaValue PrepareFunctionCached() =>
            _cachedFunctionScript.PrepareFunction(_functionSource).Function;

        /// <summary>
        /// Prepares a named standalone function already present in the script compilation cache.
        /// </summary>
        [Benchmark(Description = "Prepare Function Cached Named")]
        public LuaValue PrepareFunctionCachedNamed() =>
            _namedCachedFunctionScript
                .PrepareFunction(_functionSource, funcFriendlyName: _cachedFunctionFriendlyName)
                .Function;

        /// <summary>
        /// Executes a chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoString Cached")]
        public LuaValue DoStringCached() => _cachedScript.DoString(_scriptSource);

        /// <summary>
        /// Executes a named chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoString Cached Named")]
        public LuaValue DoStringCachedNamed() =>
            _namedCachedScript.DoString(_scriptSource, null, _cachedFriendlyName);

        /// <summary>
        /// Executes a file-backed chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoFile Cached")]
        public LuaValue DoFileCached() => _cachedFileScript.DoFile(_cachedFileName);

        /// <summary>
        /// Executes a named file-backed chunk already present in the script compilation cache through the easy API.
        /// </summary>
        [Benchmark(Description = "DoFile Cached Named")]
        public LuaValue DoFileCachedNamed() =>
            _namedCachedFileScript.DoFile(
                _cachedFileName,
                codeFriendlyName: _cachedFileFriendlyName
            );

        /// <summary>
        /// Executes the precompiled chunk, isolating runtime overhead.
        /// </summary>
        [Benchmark(Description = "Execute Precompiled")]
        public LuaValue ExecutePrecompiled() => _precompiledScript.CallValues(_precompiledFunction);

        /// <summary>
        /// Executes the explicit prepare-once handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Prepared String Handle")]
        public LuaValue ExecutePreparedStringHandle() => _compiledHandle.Execute();

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
        public LuaValue ExecutePreparedStreamHandle() => _compiledStreamHandle.Execute();

        /// <summary>
        /// Executes a file-prepared handle, isolating handle forwarding overhead.
        /// </summary>
        [Benchmark(Description = "Execute Prepared File Handle")]
        public LuaValue ExecutePreparedFileHandle() => _compiledFileHandle.Execute();

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
        private LuaValue _function = LuaValue.Nil;
        private LuaValue _nestedFunction = LuaValue.Nil;
        private LuaValue _sixArgFunction = LuaValue.Nil;
        private LuaValue _sevenArgFunction = LuaValue.Nil;
        private LuaValue _zeroArgFunction = LuaValue.Nil;
        private LuaValue _doubleArgFunction = LuaValue.Nil;
        private object[] _nestedFunctionPath = Array.Empty<object>();
        private object[] _paddedNestedFunctionPath = Array.Empty<object>();
        private LuaValue _arg1 = LuaValue.Nil;
        private LuaValue _arg2 = LuaValue.Nil;
        private LuaValue _arg3 = LuaValue.Nil;
        private LuaValue _arg4 = LuaValue.Nil;
        private LuaValue _arg5 = LuaValue.Nil;
        private LuaValue _arg6 = LuaValue.Nil;
        private LuaValue _arg7 = LuaValue.Nil;
        private LuaValue _doubleDynValueArg = LuaValue.Nil;
        private double _doubleArg;
        private object _boxedDoubleArg;
        private CompiledScript _boundGlobalHandle;
        private CompiledScript _boundNestedGlobalHandle;
        private CompiledScript _boundSixArgHandle;
        private CompiledScript _boundSevenArgHandle;
        private CompiledScript _boundZeroArgHandle;
        private CompiledScript _boundDoubleArgHandle;

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
                    + "function updateDouble(dt) return dt + 1 end; "
                    + "api = { system = { update = function(a, b, c) return a + b + c end } }"
            );
            _function = _script.Globals.Get("update");
            _nestedFunction = _script.Globals.Get("api", "system", "update");
            _sixArgFunction = _script.Globals.Get("update6");
            _sevenArgFunction = _script.Globals.Get("update7");
            _zeroArgFunction = _script.Globals.Get("tick");
            _doubleArgFunction = _script.Globals.Get("updateDouble");
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
            _boundDoubleArgHandle = _script.PrepareGlobalFunction("updateDouble");
            _arg1 = LuaValue.FromNumber(1);
            _arg2 = LuaValue.FromNumber(2);
            _arg3 = LuaValue.FromNumber(3);
            _arg4 = LuaValue.FromNumber(4);
            _arg5 = LuaValue.FromNumber(5);
            _arg6 = LuaValue.FromNumber(6);
            _arg7 = LuaValue.FromNumber(7);
            _doubleArg = 1.25d;
            _boxedDoubleArg = _doubleArg;
            _doubleDynValueArg = LuaValue.FromNumber(_doubleArg);
        }

        /// <summary>
        /// Resolves a global function on every call before invoking it.
        /// </summary>
        [Benchmark(Description = "Call Global Lookup")]
        public LuaValue CallGlobalLookup() =>
            _script.CallValues(_script.Globals.Get("update"), _arg1, _arg2, _arg3);

        /// <summary>
        /// Resolves a nested global function on every call before invoking it.
        /// </summary>
        [Benchmark(Description = "Call Nested Global Lookup")]
        public LuaValue CallNestedGlobalLookup() =>
            _script.CallValues(_script.Globals.Get("api", "system", "update"), _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Global")]
        public LuaValue CallCachedGlobal() => _script.CallValues(_function, _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached nested global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Nested Global")]
        public LuaValue CallCachedNestedGlobal() =>
            _script.CallValues(_nestedFunction, _arg1, _arg2, _arg3);

        /// <summary>
        /// Calls a manually cached six-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached 6-Arg Global")]
        public LuaValue CallCachedSixArgGlobal() =>
            _script.CallValues(_sixArgFunction, _arg1, _arg2, _arg3, _arg4, _arg5, _arg6);

        /// <summary>
        /// Calls a manually cached seven-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached 7-Arg Global")]
        public LuaValue CallCachedSevenArgGlobal() =>
            _script.CallValues(_sevenArgFunction, _arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);

        /// <summary>
        /// Calls a manually cached zero-argument global function value.
        /// </summary>
        [Benchmark(Description = "Call Cached Zero-Arg Global")]
        public LuaValue CallCachedZeroArgGlobal() => _script.CallValues(_zeroArgFunction);

        /// <summary>
        /// Calls a manually cached one-argument global function with a cached LuaValue.
        /// </summary>
        [Benchmark(Description = "Call Cached Double LuaValue Global")]
        public LuaValue CallCachedDoubleDynValueGlobal() =>
            _script.CallValues(_doubleArgFunction, _doubleDynValueArg);

        /// <summary>
        /// Executes a global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Global Handle")]
        public LuaValue ExecutePreparedGlobalHandle() =>
            _boundGlobalHandle.ExecuteValues(_arg1, _arg2, _arg3);

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
        public LuaValue ExecutePreparedNestedGlobalHandle() =>
            _boundNestedGlobalHandle.ExecuteValues(_arg1, _arg2, _arg3);

        /// <summary>
        /// Executes a six-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared 6-Arg Handle")]
        public LuaValue ExecutePreparedSixArgHandle() =>
            _boundSixArgHandle.ExecuteValues(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6);

        /// <summary>
        /// Executes a seven-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared 7-Arg Handle")]
        public LuaValue ExecutePreparedSevenArgHandle() =>
            _boundSevenArgHandle.ExecuteValues(_arg1, _arg2, _arg3, _arg4, _arg5, _arg6, _arg7);

        /// <summary>
        /// Executes a zero-argument global function handle resolved once through the public prepare API.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Zero-Arg Handle")]
        public LuaValue ExecutePreparedZeroArgHandle() => _boundZeroArgHandle.Execute();

        /// <summary>
        /// Executes a one-argument global function handle with a cached LuaValue argument.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Double LuaValue Handle")]
        public LuaValue ExecutePreparedDoubleDynValueHandle() =>
            _boundDoubleArgHandle.ExecuteValues(_doubleDynValueArg);

        /// <summary>
        /// Executes a one-argument global function handle through the primitive double overload.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Double Primitive Handle")]
        public LuaValue ExecutePreparedDoublePrimitiveHandle() =>
            _boundDoubleArgHandle.Execute(_doubleArg);

        /// <summary>
        /// Executes a one-argument global function handle through the forced object convenience path.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Double Object Handle")]
        public LuaValue ExecutePreparedDoubleObjectHandle() =>
            _boundDoubleArgHandle.Execute(_boxedDoubleArg);

        /// <summary>
        /// Executes a one-argument global function handle through the object convenience path with
        /// per-call boxing of the primitive input.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Double Boxed Object Handle")]
        public LuaValue ExecutePreparedDoubleBoxedObjectHandle() =>
            _boundDoubleArgHandle.Execute((object)_doubleArg);

        /// <summary>
        /// Executes a one-argument global function handle through the primitive double overload and
        /// strict numeric result helper.
        /// </summary>
        [Benchmark(Description = "Execute Prepared Double Primitive Handle Number")]
        public double ExecutePreparedDoublePrimitiveHandleNumber() =>
            _boundDoubleArgHandle.ExecuteNumber(_doubleArg);

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
