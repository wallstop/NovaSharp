namespace WallstopStudios.NovaSharp.Interpreter
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using CoreLib;
    using Cysharp.Text;
    using Debugging;
    using Diagnostics;
    using Platforms;
    using Tree.Expressions;
    using Tree.FastInterface;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Execution;
    using WallstopStudios.NovaSharp.Interpreter.Execution.Scopes;
    using WallstopStudios.NovaSharp.Interpreter.Execution.VM;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure;
    using WallstopStudios.NovaSharp.Interpreter.Infrastructure.IO;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// This class implements a NovaSharp scripting session. Multiple Script objects can coexist in the same program but cannot share
    /// data among themselves unless some mechanism is put in place.
    /// </summary>
    public class Script : IScriptPrivateResource
    {
        /// <summary>
        /// The version of the NovaSharp engine
        /// </summary>
        public const string VERSION = "2.0.0.0";

        /// <summary>
        /// The default Lua version targeted by NovaSharp.
        /// </summary>
        public const string LuaVersion = "5.4";

        private readonly Processor _mainProcessor;
        private readonly ByteCode _byteCode;
        private readonly List<SourceCode> _sources = new();
        private readonly Table _globalTable;
        private IDebugger _debugger;
        private readonly Table[] _typeMetatables = new Table[(int)LuaTypeExtensions.MaxMetaTypes];
        private readonly ITimeProvider _timeProvider;
        private readonly IRandomProvider _randomProvider;
        private readonly DateTime _startTimeUtc;
        private readonly Sandboxing.AllocationTracker _allocationTracker;
        private readonly Execution.ScriptCompilationCache _compilationCache;
        private bool _bit32CompatibilityWarningEmitted;
        private static ScriptGlobalOptions GlobalOptionsSnapshot;
        private static readonly AsyncLocal<GlobalOptionsScope> ScopedGlobalOptions = new();
        private static readonly ConcurrentDictionary<
            Type,
            MethodInfo
        > LegacyResolveFileNameMethods = new();
        private static readonly Func<LoadStringGuardState, DynValue> LoadStringGuardAction =
            static state =>
                state.Script.LoadStringCore(
                    state.Code,
                    state.GlobalTable,
                    state.CodeFriendlyName,
                    state.SkipCompilationCacheLookup
                );
        private static readonly Func<LoadStreamGuardState, DynValue> LoadStreamGuardAction =
            static state =>
                state.Script.LoadStreamCore(
                    state.Stream,
                    state.GlobalTable,
                    state.CodeFriendlyName
                );

        /// <summary>
        /// Carries state through the compatibility guard for string loads without capturing a closure.
        /// </summary>
        private readonly struct LoadStringGuardState
        {
            /// <summary>
            /// Initializes a new string load guard state.
            /// </summary>
            /// <param name="script">Script instance that owns the load operation.</param>
            /// <param name="code">Lua source text to load.</param>
            /// <param name="globalTable">Optional environment table for the loaded closure.</param>
            /// <param name="codeFriendlyName">Optional debugger-facing chunk name.</param>
            /// <param name="skipCompilationCacheLookup">Whether to skip the compilation cache lookup before compiling.</param>
            public LoadStringGuardState(
                Script script,
                string code,
                Table globalTable,
                string codeFriendlyName,
                bool skipCompilationCacheLookup
            )
            {
                Script = script;
                Code = code;
                GlobalTable = globalTable;
                CodeFriendlyName = codeFriendlyName;
                SkipCompilationCacheLookup = skipCompilationCacheLookup;
            }

            /// <summary>
            /// Gets the script instance that owns the load operation.
            /// </summary>
            public Script Script { get; }

            /// <summary>
            /// Gets the Lua source text to load.
            /// </summary>
            public string Code { get; }

            /// <summary>
            /// Gets the optional environment table for the loaded closure.
            /// </summary>
            public Table GlobalTable { get; }

            /// <summary>
            /// Gets the optional debugger-facing chunk name.
            /// </summary>
            public string CodeFriendlyName { get; }

            /// <summary>
            /// Gets whether to skip the compilation cache lookup before compiling.
            /// </summary>
            public bool SkipCompilationCacheLookup { get; }
        }

        /// <summary>
        /// Carries state through the compatibility guard for stream loads without capturing a closure.
        /// </summary>
        private readonly struct LoadStreamGuardState
        {
            /// <summary>
            /// Initializes a new stream load guard state.
            /// </summary>
            /// <param name="script">Script instance that owns the load operation.</param>
            /// <param name="stream">Stream containing Lua source or dumped bytecode.</param>
            /// <param name="globalTable">Optional environment table for the loaded closure.</param>
            /// <param name="codeFriendlyName">Optional debugger-facing chunk name.</param>
            public LoadStreamGuardState(
                Script script,
                Stream stream,
                Table globalTable,
                string codeFriendlyName
            )
            {
                Script = script;
                Stream = stream;
                GlobalTable = globalTable;
                CodeFriendlyName = codeFriendlyName;
            }

            /// <summary>
            /// Gets the script instance that owns the load operation.
            /// </summary>
            public Script Script { get; }

            /// <summary>
            /// Gets the stream containing Lua source or dumped bytecode.
            /// </summary>
            public Stream Stream { get; }

            /// <summary>
            /// Gets the optional environment table for the loaded closure.
            /// </summary>
            public Table GlobalTable { get; }

            /// <summary>
            /// Gets the optional debugger-facing chunk name.
            /// </summary>
            public string CodeFriendlyName { get; }
        }

        private readonly struct FixedChainedCallArguments
        {
            private readonly DynValue _arg0;
            private readonly DynValue _arg1;
            private readonly DynValue _arg2;
            private readonly DynValue _arg3;
            private readonly DynValue _arg4;
            private readonly DynValue _arg5;
            private readonly DynValue _arg6;

            internal FixedChainedCallArguments(DynValue arg0)
            {
                _arg0 = arg0;
                _arg1 = null;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                Count = 1;
            }

            internal FixedChainedCallArguments(DynValue arg0, DynValue arg1)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = null;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                Count = 2;
            }

            internal FixedChainedCallArguments(DynValue arg0, DynValue arg1, DynValue arg2)
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = null;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                Count = 3;
            }

            internal FixedChainedCallArguments(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = null;
                _arg5 = null;
                _arg6 = null;
                Count = 4;
            }

            internal FixedChainedCallArguments(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = arg4;
                _arg5 = null;
                _arg6 = null;
                Count = 5;
            }

            internal FixedChainedCallArguments(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = arg4;
                _arg5 = arg5;
                _arg6 = null;
                Count = 6;
            }

            internal FixedChainedCallArguments(
                DynValue arg0,
                DynValue arg1,
                DynValue arg2,
                DynValue arg3,
                DynValue arg4,
                DynValue arg5,
                DynValue arg6
            )
            {
                _arg0 = arg0;
                _arg1 = arg1;
                _arg2 = arg2;
                _arg3 = arg3;
                _arg4 = arg4;
                _arg5 = arg5;
                _arg6 = arg6;
                Count = 7;
            }

            /// <summary>
            /// Gets the number of fixed arguments currently stored.
            /// </summary>
            internal int Count { get; }

            /// <summary>
            /// Gets a fixed argument by zero-based index.
            /// </summary>
            internal DynValue this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => _arg0,
                        1 => _arg1,
                        2 => _arg2,
                        3 => _arg3,
                        4 => _arg4,
                        5 => _arg5,
                        6 => _arg6,
                        _ => throw new ArgumentOutOfRangeException(nameof(index)),
                    };
                }
            }

            /// <summary>
            /// Prepends a callable self value when the fixed argument buffer has capacity.
            /// </summary>
            internal bool TryPrepend(DynValue value, out FixedChainedCallArguments args)
            {
                switch (Count)
                {
                    case 1:
                        args = new FixedChainedCallArguments(value, _arg0);
                        return true;
                    case 2:
                        args = new FixedChainedCallArguments(value, _arg0, _arg1);
                        return true;
                    case 3:
                        args = new FixedChainedCallArguments(value, _arg0, _arg1, _arg2);
                        return true;
                    case 4:
                        args = new FixedChainedCallArguments(value, _arg0, _arg1, _arg2, _arg3);
                        return true;
                    case 5:
                        args = new FixedChainedCallArguments(
                            value,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4
                        );
                        return true;
                    case 6:
                        args = new FixedChainedCallArguments(
                            value,
                            _arg0,
                            _arg1,
                            _arg2,
                            _arg3,
                            _arg4,
                            _arg5
                        );
                        return true;
                    default:
                        args = default;
                        return false;
                }
            }
        }

        /// <summary>
        /// Initializes the <see cref="Script"/> class.
        /// </summary>
        static Script()
        {
            GlobalOptions = new ScriptGlobalOptions();

            DefaultOptions = new ScriptOptions()
            {
                DebugPrint = s =>
                {
                    GlobalOptions.Platform.DefaultPrint(s);
                },
                DebugInput = s =>
                {
                    return GlobalOptions.Platform.DefaultInput(s);
                },
                CheckThreadAccess = true,
                ScriptLoader = PlatformAutoDetector.GetDefaultScriptLoader(),
                TailCallOptimizationThreshold = 65536,
            };
            DefaultOptions.CompatibilityVersion = GlobalOptions.CompatibilityVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class with default modules
        /// and the Lua version from <see cref="GlobalOptions"/>.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="CoreModulePresets.Default"/> and inherits <see cref="ScriptGlobalOptions.CompatibilityVersion"/>
        /// from <see cref="GlobalOptions"/>. For explicit version control, use <see cref="Script(LuaCompatibilityVersion)"/>
        /// or <see cref="Script(LuaCompatibilityVersion, CoreModules)"/>.
        /// </remarks>
        public Script()
            : this(CoreModulePresets.Default, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class with the specified Lua version
        /// and default modules.
        /// </summary>
        /// <param name="version">The Lua compatibility version to target.</param>
        /// <remarks>
        /// Uses <see cref="CoreModulePresets.Default"/>. This is the recommended constructor when you need
        /// explicit version control without custom modules.
        /// </remarks>
        public Script(LuaCompatibilityVersion version)
            : this(version, CoreModulePresets.Default) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class with the specified Lua version
        /// and core modules.
        /// </summary>
        /// <param name="version">The Lua compatibility version to target.</param>
        /// <param name="coreModules">The core modules to be pre-registered in the default global table.</param>
        /// <remarks>
        /// This is the recommended constructor for explicit control over both version and modules.
        /// </remarks>
        public Script(LuaCompatibilityVersion version, CoreModules coreModules)
            : this(coreModules, CreateOptionsForVersion(version)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class with the specified core modules
        /// and the Lua version from <see cref="GlobalOptions"/>.
        /// </summary>
        /// <param name="coreModules">The core modules to be pre-registered in the default global table.</param>
        /// <remarks>
        /// Inherits <see cref="ScriptGlobalOptions.CompatibilityVersion"/> from <see cref="GlobalOptions"/>.
        /// For explicit version control, use <see cref="Script(LuaCompatibilityVersion, CoreModules)"/>.
        /// </remarks>
        public Script(CoreModules coreModules)
            : this(coreModules, null) { }

        /// <summary>
        /// Initializes a new instance using a custom options snapshot with default modules.
        /// </summary>
        /// <param name="options">
        /// The options to use. The <see cref="ScriptOptions.CompatibilityVersion"/> from these options
        /// is used as-is (unlike the parameterless constructor which inherits from <see cref="GlobalOptions"/>).
        /// </param>
        /// <remarks>
        /// Uses <see cref="CoreModulePresets.Default"/>. Note that if you create a fresh <see cref="ScriptOptions"/>
        /// without copying from <see cref="DefaultOptions"/>, the version defaults to <see cref="LuaCompatibilityVersion.Latest"/>.
        /// </remarks>
        public Script(ScriptOptions options)
            : this(CoreModulePresets.Default, options) { }

        /// <summary>
        /// Initializes a new instance with the specified modules and options.
        /// </summary>
        /// <param name="coreModules">The core modules to be pre-registered in the default global table.</param>
        /// <param name="options">
        /// The options to use, or <c>null</c> to use <see cref="DefaultOptions"/> with
        /// <see cref="ScriptGlobalOptions.CompatibilityVersion"/> from <see cref="GlobalOptions"/>.
        /// </param>
        /// <remarks>
        /// This is the most flexible constructor. When <paramref name="options"/> is <c>null</c>,
        /// the script inherits <see cref="ScriptGlobalOptions.CompatibilityVersion"/> from <see cref="GlobalOptions"/>.
        /// When <paramref name="options"/> is provided, its <see cref="ScriptOptions.CompatibilityVersion"/> is used as-is.
        /// </remarks>
        public Script(CoreModules coreModules, ScriptOptions options)
        {
            Options = new ScriptOptions(options ?? DefaultOptions);

            if (options == null)
            {
                Options.CompatibilityVersion = GlobalOptions.CompatibilityVersion;
            }

            _timeProvider = Options.TimeProvider ?? SystemTimeProvider.Instance;
            _randomProvider =
                Options.RandomProvider ?? CreateDefaultRandomProvider(Options.CompatibilityVersion);
            _startTimeUtc = _timeProvider.GetUtcNow().UtcDateTime;

            // Initialize allocation tracker if memory or coroutine limits are configured
            if (Options.Sandbox.HasMemoryLimit || Options.Sandbox.HasCoroutineLimit)
            {
                _allocationTracker = new Sandboxing.AllocationTracker();
            }

            // Initialize compilation cache if enabled
            if (Options.EnableScriptCaching)
            {
                if (Options.ScriptCacheMaxEntries < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(options),
                        Options.ScriptCacheMaxEntries,
                        "ScriptOptions.ScriptCacheMaxEntries cannot be negative."
                    );
                }

                _compilationCache = new Execution.ScriptCompilationCache(
                    Options.ScriptCacheMaxEntries
                );
            }

            PerformanceStats = new PerformanceStatistics(
                Options.HighResolutionClock ?? SystemHighResolutionClock.Instance
            );
            Registry = new Table(this);

            _byteCode = new ByteCode(this);
            _mainProcessor = new Processor(this, _globalTable, _byteCode);
            _globalTable = new Table(this).RegisterCoreModules(coreModules);
        }

        /// <summary>
        /// Creates the default random provider based on the Lua compatibility version.
        /// Lua 5.4+ uses xoshiro256** (via <see cref="LuaRandomProvider"/>),
        /// while Lua 5.1-5.3 use a Linear Congruential Generator (via <see cref="Lua51RandomProvider"/>).
        /// </summary>
        /// <param name="version">The Lua compatibility version.</param>
        /// <returns>The appropriate random provider for the version.</returns>
        private static IRandomProvider CreateDefaultRandomProvider(LuaCompatibilityVersion version)
        {
            // Resolve Latest to the current default version
            LuaCompatibilityVersion effectiveVersion = LuaVersionDefaults.Resolve(version);

            // Lua 5.4+ uses xoshiro256** (our LuaRandomProvider)
            // Lua 5.1-5.3 used C library rand() which we emulate with LCG (Lua51RandomProvider)
            if (effectiveVersion >= LuaCompatibilityVersion.Lua54)
            {
                return new LuaRandomProvider();
            }

            return new Lua51RandomProvider();
        }

        /// <summary>
        /// Creates a <see cref="ScriptOptions"/> instance configured for the specified Lua version.
        /// </summary>
        /// <param name="version">The Lua compatibility version to target.</param>
        /// <returns>A new <see cref="ScriptOptions"/> with the specified version.</returns>
        private static ScriptOptions CreateOptionsForVersion(LuaCompatibilityVersion version)
        {
            return new ScriptOptions(DefaultOptions) { CompatibilityVersion = version };
        }

        /// <summary>
        /// Gets or sets the script loader which will be used as the value of the
        /// ScriptLoader property for all newly created scripts.
        /// </summary>
        public static ScriptOptions DefaultOptions { get; private set; }

        /// <summary>
        /// Gets access to the script options.
        /// </summary>
        public ScriptOptions Options { get; private set; }

        /// <summary>
        /// Gets the global options, that is options which cannot be customized per-script.
        /// </summary>
        public static ScriptGlobalOptions GlobalOptions
        {
            get { return ScopedGlobalOptions.Value?.Options ?? GlobalOptionsSnapshot; }
            internal set
            {
                if (ScopedGlobalOptions.Value != null)
                {
                    ScopedGlobalOptions.Value.Options =
                        value ?? throw new ArgumentNullException(nameof(value));
                }
                else
                {
                    GlobalOptionsSnapshot = value ?? throw new ArgumentNullException(nameof(value));
                }
            }
        }

        /// <summary>
        /// Gets the effective Lua compatibility version for this script.
        /// </summary>
        public LuaCompatibilityVersion CompatibilityVersion
        {
            get { return Options.CompatibilityVersion; }
        }

        /// <summary>
        /// Gets the derived compatibility profile describing the selected Lua feature set.
        /// </summary>
        public LuaCompatibilityProfile CompatibilityProfile =>
            LuaCompatibilityProfile.ForVersion(CompatibilityVersion);

        /// <summary>
        /// Captures the current global options and returns a scope that restores them when disposed.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that reverts <see cref="GlobalOptions"/> to its previous value.</returns>
        internal static IDisposable BeginGlobalOptionsScope()
        {
            GlobalOptionsScope scope = new(GlobalOptions.Clone(), ScopedGlobalOptions.Value);
            ScopedGlobalOptions.Value = scope;
            return scope;
        }

        internal static IDisposable BeginDefaultOptionsScope()
        {
            return new DefaultOptionsScope(DefaultOptions);
        }

        /// <summary>
        /// Gets access to performance statistics.
        /// </summary>
        public PerformanceStatistics PerformanceStats { get; internal set; }

        /// <summary>
        /// Gets the allocation tracker for this script, or <c>null</c> if memory tracking is not enabled.
        /// Memory tracking is enabled when <see cref="ScriptOptions.Sandbox"/> has a non-zero <see cref="Sandboxing.SandboxOptions.MaxMemoryBytes"/>.
        /// </summary>
        public Sandboxing.AllocationTracker AllocationTracker => _allocationTracker;

        /// <summary>
        /// Gets the time provider associated with this script.
        /// </summary>
        public ITimeProvider TimeProvider => _timeProvider;

        /// <summary>
        /// Gets the random number provider associated with this script.
        /// Used by <c>math.random</c> and <c>math.randomseed</c>.
        /// </summary>
        public IRandomProvider RandomProvider => _randomProvider;

        /// <summary>
        /// Gets the UTC timestamp captured from <see cref="TimeProvider"/> when the script was constructed.
        /// </summary>
        internal DateTime StartTimeUtc => _startTimeUtc;

        /// <summary>
        /// Gets the approximate number of compiled scripts currently in the cache.
        /// Returns 0 if caching is disabled via <see cref="ScriptOptions.EnableScriptCaching"/>.
        /// </summary>
        public int CompilationCacheCount => _compilationCache?.ApproximateCount ?? 0;

        /// <summary>
        /// Clears the script compilation cache, forcing subsequent <see cref="LoadString"/> calls
        /// to perform full compilation. Has no effect if caching is disabled.
        /// </summary>
        public void ClearCompilationCache()
        {
            _compilationCache?.Clear();
        }

        /// <summary>
        /// Gets the default global table for this script. Unless a different table is intentionally passed (or setfenv has been used)
        /// execution uses this table.
        /// </summary>
        public Table Globals
        {
            get { return _globalTable; }
        }

        /// <summary>
        /// Loads a string containing a Lua/NovaSharp function.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="funcFriendlyName">Name of the function used to report errors, etc.</param>
        /// <returns>
        /// A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadFunction(
            string code,
            Table globalTable = null,
            string funcFriendlyName = null
        )
        {
            this.CheckScriptOwnership(globalTable);

            string chunkName = ZString.Concat(
                "libfunc_",
                funcFriendlyName ?? _sources.Count.ToString(CultureInfo.InvariantCulture)
            );

            SourceCode source = new(chunkName, code, _sources.Count, this);

            _sources.Add(source);

            int address = LoaderFast.LoadFunction(
                this,
                source,
                _byteCode,
                globalTable != null || _globalTable != null
            );

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            return MakeClosure(address, globalTable ?? _globalTable);
        }

        /// <summary>
        /// Compiles a string containing a Lua/NovaSharp function and returns an executable handle.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this function.</param>
        /// <param name="funcFriendlyName">Name of the function used to report errors, etc.</param>
        /// <returns>A compiled handle that can be executed repeatedly without source text.</returns>
        public CompiledScript CompileFunction(
            string code,
            Table globalTable = null,
            string funcFriendlyName = null
        )
        {
            return new CompiledScript(this, LoadFunction(code, globalTable, funcFriendlyName));
        }

        private void SignalByteCodeChange()
        {
            if (_debugger != null)
            {
                int instructionCount = _byteCode.Code.Count;
                string[] instructions = new string[instructionCount];

                for (int i = 0; i < instructionCount; i++)
                {
                    instructions[i] = _byteCode.Code[i].ToString();
                }

                _debugger.SetByteCode(instructions);
            }
        }

        private void SignalSourceCodeChange(SourceCode source)
        {
            if (_debugger != null)
            {
                _debugger.SetSourceCode(source);
            }
        }

        /// <summary>
        /// Loads a string containing a Lua/NovaSharp script.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc. Also used by debuggers to locate the original source file.</param>
        /// <returns>
        /// A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadString(
            string code,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            LoadStringGuardState state = new(
                this,
                code,
                globalTable,
                codeFriendlyName,
                skipCompilationCacheLookup: false
            );
            return ExecuteWithCompatibilityGuard(state, LoadStringGuardAction);
        }

        private DynValue LoadStringSkippingCompilationCacheLookup(
            string code,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            LoadStringGuardState state = new(
                this,
                code,
                globalTable,
                codeFriendlyName,
                skipCompilationCacheLookup: true
            );
            return ExecuteWithCompatibilityGuard(state, LoadStringGuardAction);
        }

        /// <summary>
        /// Compiles a string containing a Lua/NovaSharp script and returns an executable handle.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc. Also used by debuggers to locate the original source file.</param>
        /// <returns>A compiled handle that can be executed repeatedly without source text.</returns>
        public CompiledScript CompileString(
            string code,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            return new CompiledScript(this, LoadString(code, globalTable, codeFriendlyName));
        }

        /// <summary>
        /// Creates an executable handle for a function or callable value that has already been
        /// resolved for this script.
        /// </summary>
        /// <param name="function">The function or callable value to bind.</param>
        /// <returns>An executable handle that can be called repeatedly without resolving the value again.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="function"/> is not directly callable and has no callable
        /// <c>__call</c> metamethod.
        /// </exception>
        public CompiledScript BindFunction(DynValue function)
        {
            return new CompiledScript(this, function);
        }

        /// <summary>
        /// Resolves a global function once and returns an executable handle for repeated calls.
        /// </summary>
        /// <param name="name">The global function name.</param>
        /// <returns>An executable handle for the current global value.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/> is null or empty, or when the global value is not
        /// callable.
        /// </exception>
        public CompiledScript BindGlobalFunction(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    "Global function name cannot be null or empty.",
                    nameof(name)
                );
            }

            return BindFunction(Globals.Get(name));
        }

        /// <summary>
        /// Validates that a compiled handle target belongs to this script and can be called.
        /// </summary>
        /// <param name="function">The function or callable value to validate.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="function"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="function"/> is not directly callable and has no callable
        /// <c>__call</c> metamethod.
        /// </exception>
        internal void ValidateCompiledScriptTarget(DynValue function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);

            if (!IsDirectCallTarget(function))
            {
                _ = GetCallableMetamethodOrThrow(function);
            }
        }

        /// <summary>
        /// Executes a resolved callable handle without re-running the public call dispatcher when
        /// the handle is a Lua function.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(DynValue function)
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function);
        }

        /// <summary>
        /// Executes a resolved callable handle with one argument.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(DynValue function, DynValue arg)
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg);
        }

        /// <summary>
        /// Executes a resolved callable handle with two arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(DynValue function, DynValue arg1, DynValue arg2)
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg1, arg2);
        }

        /// <summary>
        /// Executes a resolved callable handle with three arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3
        )
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg1, arg2, arg3);
        }

        /// <summary>
        /// Executes a resolved callable handle with four arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        /// Executes a resolved callable handle with five arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        /// Executes a resolved callable handle with six arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        /// <summary>
        /// Executes a resolved callable handle with seven arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(
                function,
                arg1,
                arg2,
                arg3,
                arg4,
                arg5,
                arg6,
                arg7
            );
        }

        /// <summary>
        /// Executes a resolved callable handle with caller-owned contiguous arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(DynValue function, ReadOnlySpan<DynValue> args)
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, args);
        }

        /// <summary>
        /// Executes a resolved callable handle with caller-owned contiguous CLR object arguments.
        /// </summary>
        internal DynValue ExecuteCompiledFunction(DynValue function, ReadOnlySpan<object> args)
        {
            this.CheckScriptOwnership(function);
            return ExecuteTrustedCompiledFunction(function, args);
        }

        /// <summary>
        /// Executes a handle-created callable after the handle creation path has already validated
        /// the stored function's ownership and callability.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(DynValue function)
        {
            if (function.Type != DataType.Function)
            {
                return Call(function);
            }

            return ExecuteLuaFunctionWithoutArguments(function);
        }

        private DynValue ExecuteLuaFunctionWithoutArguments(DynValue function)
        {
            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function),
                static state => state._mainProcessor.CallFunctionWithoutArguments(state.function)
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with one caller-provided argument.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(DynValue function, DynValue arg)
        {
            this.CheckScriptOwnership(arg);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg),
                static state => state._mainProcessor.Call(state.function, state.arg)
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with two caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2),
                static state => state._mainProcessor.Call(state.function, state.arg1, state.arg2)
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with three caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2, arg3);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3),
                static state =>
                    state._mainProcessor.Call(state.function, state.arg1, state.arg2, state.arg3)
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with four caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2, arg3, arg4);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4
                    )
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with five caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2, arg3, arg4, arg5);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5
                    )
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with six caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6
                    )
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with seven caller-provided arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);
            this.CheckScriptOwnership(arg7);

            if (function.Type != DataType.Function)
            {
                return Call(function, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6,
                        state.arg7
                    )
            );
        }

        /// <summary>
        /// Executes a trusted handle-created callable with caller-owned contiguous arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            ReadOnlySpan<DynValue> args
        )
        {
            this.CheckScriptOwnership(args);

            if (function.Type != DataType.Function)
            {
                return Call(function, args);
            }

            return args.Length switch
            {
                0 => ExecuteTrustedCompiledFunction(function),
                1 => ExecuteWithCompatibilityGuard(
                    (_mainProcessor, function, arg: args[0]),
                    static state => state._mainProcessor.Call(state.function, state.arg)
                ),
                2 => ExecuteWithCompatibilityGuard(
                    (_mainProcessor, function, arg1: args[0], arg2: args[1]),
                    static state =>
                        state._mainProcessor.Call(state.function, state.arg1, state.arg2)
                ),
                3 => ExecuteWithCompatibilityGuard(
                    (_mainProcessor, function, arg1: args[0], arg2: args[1], arg3: args[2]),
                    static state =>
                        state._mainProcessor.Call(
                            state.function,
                            state.arg1,
                            state.arg2,
                            state.arg3
                        )
                ),
                4 => ExecuteWithCompatibilityGuard(
                    (
                        _mainProcessor,
                        function,
                        arg1: args[0],
                        arg2: args[1],
                        arg3: args[2],
                        arg4: args[3]
                    ),
                    static state =>
                        state._mainProcessor.Call(
                            state.function,
                            state.arg1,
                            state.arg2,
                            state.arg3,
                            state.arg4
                        )
                ),
                5 => ExecuteWithCompatibilityGuard(
                    (
                        _mainProcessor,
                        function,
                        arg1: args[0],
                        arg2: args[1],
                        arg3: args[2],
                        arg4: args[3],
                        arg5: args[4]
                    ),
                    static state =>
                        state._mainProcessor.Call(
                            state.function,
                            state.arg1,
                            state.arg2,
                            state.arg3,
                            state.arg4,
                            state.arg5
                        )
                ),
                6 => ExecuteWithCompatibilityGuard(
                    (
                        _mainProcessor,
                        function,
                        arg1: args[0],
                        arg2: args[1],
                        arg3: args[2],
                        arg4: args[3],
                        arg5: args[4],
                        arg6: args[5]
                    ),
                    static state =>
                        state._mainProcessor.Call(
                            state.function,
                            state.arg1,
                            state.arg2,
                            state.arg3,
                            state.arg4,
                            state.arg5,
                            state.arg6
                        )
                ),
                7 => ExecuteWithCompatibilityGuard(
                    (
                        _mainProcessor,
                        function,
                        arg1: args[0],
                        arg2: args[1],
                        arg3: args[2],
                        arg4: args[3],
                        arg5: args[4],
                        arg6: args[5],
                        arg7: args[6]
                    ),
                    static state =>
                        state._mainProcessor.Call(
                            state.function,
                            state.arg1,
                            state.arg2,
                            state.arg3,
                            state.arg4,
                            state.arg5,
                            state.arg6,
                            state.arg7
                        )
                ),
                _ => ExecuteSpanCallWithCompatibilityGuard(function, args),
            };
        }

        /// <summary>
        /// Executes a trusted handle-created callable with caller-owned contiguous CLR object arguments.
        /// </summary>
        internal DynValue ExecuteTrustedCompiledFunction(
            DynValue function,
            ReadOnlySpan<object> args
        )
        {
            if (function.Type != DataType.Function)
            {
                return CallObjectArguments(function, args);
            }

            switch (args.Length)
            {
                case 0:
                    return ExecuteTrustedCompiledFunction(function);
                case 1:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0])
                    );
                case 2:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1])
                    );
                case 3:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2])
                    );
                case 4:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3])
                    );
                case 5:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4])
                    );
                case 6:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4]),
                        DynValue.FromObject(this, args[5])
                    );
                case 7:
                    return ExecuteTrustedCompiledFunction(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4]),
                        DynValue.FromObject(this, args[5]),
                        DynValue.FromObject(this, args[6])
                    );
            }

            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                args.Length,
                out DynValue[] convertedArgs
            );
            for (int i = 0; i < args.Length; i++)
            {
                convertedArgs[i] = DynValue.FromObject(this, args[i]);
            }

            return ExecuteTrustedCompiledFunction(function, convertedArgs.AsSpan(0, args.Length));
        }

        private DynValue LoadStringCore(
            string code,
            Table globalTable = null,
            string codeFriendlyName = null,
            bool skipCompilationCacheLookup = false
        )
        {
            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            this.CheckScriptOwnership(globalTable);

            if (code.StartsWith(StringModule.Base64DumpHeader, StringComparison.Ordinal))
            {
                code = code.Substring(StringModule.Base64DumpHeader.Length);
                byte[] data = Convert.FromBase64String(code);
                using MemoryStream ms = new(data);
                return LoadStream(ms, globalTable, codeFriendlyName);
            }

            LuaCompatibilityVersion compatibilityVersion = Options.CompatibilityVersion;

            // Try to use cached compilation if available. Named chunks must match by name
            // because emitted bytecode stores SourceRef instances for diagnostics.
            if (
                !skipCompilationCacheLookup
                && _compilationCache != null
                && _compilationCache.TryGet(
                    code,
                    compatibilityVersion,
                    codeFriendlyName,
                    out Execution.CachedChunk cached
                )
            )
            {
                // Cache hit: reuse the previously compiled bytecode
                // The SourceCode is already in _sources at cached.SourceId
                // Just create a new closure pointing to the cached entry point
                return MakeClosure(cached._entryPointAddress, globalTable ?? _globalTable);
            }

            string chunkName =
                codeFriendlyName
                ?? ZString.Concat("chunk_", _sources.Count.ToString(CultureInfo.InvariantCulture));

            SourceCode source = new(codeFriendlyName ?? chunkName, code, _sources.Count, this);

            _sources.Add(source);

            int address = LoaderFast.LoadChunk(this, source, _byteCode);

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            // Store in cache for future reuse. Anonymous chunks use a null name key so
            // repeated anonymous loads continue to share the generated first source.
            if (_compilationCache != null)
            {
                _compilationCache.Store(
                    code,
                    compatibilityVersion,
                    codeFriendlyName,
                    address,
                    source.SourceId
                );
            }

            return MakeClosure(address, globalTable ?? _globalTable);
        }

        /// <summary>
        /// Loads a Lua/NovaSharp script from a System.IO.Stream. NOTE: This will *NOT* close the stream!
        /// </summary>
        /// <param name="stream">The stream containing code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc.</param>
        /// <returns>
        /// A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadStream(
            Stream stream,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            LoadStreamGuardState state = new(this, stream, globalTable, codeFriendlyName);
            return ExecuteWithCompatibilityGuard(state, LoadStreamGuardAction);
        }

        /// <summary>
        /// Compiles a Lua/NovaSharp script from a System.IO.Stream and returns an executable handle.
        /// NOTE: This will *NOT* close the stream!
        /// </summary>
        /// <param name="stream">The stream containing code.</param>
        /// <param name="globalTable">The global table to bind to this chunk.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc.</param>
        /// <returns>A compiled handle that can be executed repeatedly without retaining the stream.</returns>
        public CompiledScript CompileStream(
            Stream stream,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            return new CompiledScript(this, LoadStream(stream, globalTable, codeFriendlyName));
        }

        private DynValue LoadStreamCore(
            Stream stream,
            Table globalTable = null,
            string codeFriendlyName = null
        )
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.CheckScriptOwnership(globalTable);

            Stream codeStream = new UndisposableStream(stream);

            if (!Processor.IsDumpStream(codeStream))
            {
                using StreamReader sr = new(codeStream);
                string scriptCode = sr.ReadToEnd();
                return LoadString(scriptCode, globalTable, codeFriendlyName);
            }

            string chunkName =
                codeFriendlyName
                ?? ZString.Concat("dump_", _sources.Count.ToString(CultureInfo.InvariantCulture));

            SourceCode source = new(
                codeFriendlyName ?? chunkName,
                ZString.Concat(
                    "-- This script was decoded from a binary dump - dump_",
                    _sources.Count
                ),
                _sources.Count,
                this
            );

            _sources.Add(source);

            int address = _mainProcessor.Undump(
                codeStream,
                _sources.Count - 1,
                globalTable ?? _globalTable,
                out bool hasUpValues
            );

            SignalSourceCodeChange(source);
            SignalByteCodeChange();

            if (hasUpValues)
            {
                return MakeClosure(address, globalTable ?? _globalTable);
            }

            return MakeClosure(address);
        }

        /// <summary>
        /// Dumps on the specified stream.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentException">
        /// function arg is not a function!
        /// or
        /// stream is readonly!
        /// or
        /// function arg has upvalues other than _ENV
        /// </exception>
        public void Dump(DynValue function, Stream stream)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            this.CheckScriptOwnership(function);

            if (function.Type != DataType.Function)
            {
                throw new ArgumentException("function arg is not a function!");
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException("stream is readonly!");
            }

            Closure.UpValuesType upvaluesType = function.Function.CapturedUpValuesType;

            if (upvaluesType == Closure.UpValuesType.Closure)
            {
                throw new ArgumentException("function arg has upvalues other than _ENV");
            }

            using (UndisposableStream outStream = new(stream))
            {
                _mainProcessor.Dump(
                    outStream,
                    function.Function.EntryPointByteCodeLocation,
                    upvaluesType == Closure.UpValuesType.Environment
                );
            }
        }

        /// <summary>
        /// Loads a string containing a Lua/NovaSharp script.
        /// </summary>
        /// <param name="filename">The code.</param>
        /// <param name="globalContext">The global table to bind to this chunk.</param>
        /// <param name="friendlyFilename">The filename to be used in error messages.</param>
        /// <returns>
        /// A DynValue containing a function which will execute the loaded code.
        /// </returns>
        public DynValue LoadFile(
            string filename,
            Table globalContext = null,
            string friendlyFilename = null
        )
        {
            object code = LoadFileContent(filename, globalContext, out string resolvedFilename);
            string chunkName = friendlyFilename ?? resolvedFilename;

            return LoadFileContent(code, globalContext, chunkName);
        }

        private object LoadFileContent(
            string filename,
            Table globalContext,
            out string resolvedFilename
        )
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            this.CheckScriptOwnership(globalContext);

            Table globals = globalContext ?? _globalTable;
            resolvedFilename = ResolveFileNameWithLegacyFallback(
                Options.ScriptLoader,
                filename,
                globals
            );

            return Options.ScriptLoader.LoadFile(resolvedFilename, globals);
        }

        private DynValue LoadFileContent(object code, Table globalContext, string chunkName)
        {
            if (code is string s)
            {
                return LoadString(s, globalContext, chunkName);
            }
            else if (code is byte[] bytes)
            {
                using MemoryStream ms = new(bytes);
                return LoadStream(ms, globalContext, chunkName);
            }
            else if (code is Stream stream)
            {
                try
                {
                    return LoadStream(stream, globalContext, chunkName);
                }
                finally
                {
                    stream.Dispose();
                }
            }
            else
            {
                if (code == null)
                {
                    throw new InvalidCastException("Unexpected null from IScriptLoader.LoadFile");
                }
                else
                {
                    throw new InvalidCastException(
                        ZString.Concat(
                            "Unsupported return type from IScriptLoader.LoadFile : ",
                            code.GetType()
                        )
                    );
                }
            }
        }

        /// <summary>
        /// Compiles a Lua/NovaSharp script from a file and returns an executable handle.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="globalContext">The global table to bind to this chunk.</param>
        /// <param name="friendlyFilename">The filename to be used in error messages.</param>
        /// <returns>A compiled handle that can be executed repeatedly without reloading the file.</returns>
        public CompiledScript CompileFile(
            string filename,
            Table globalContext = null,
            string friendlyFilename = null
        )
        {
            return new CompiledScript(this, LoadFile(filename, globalContext, friendlyFilename));
        }

        /// <summary>
        /// Loads and executes a string containing a Lua/NovaSharp script.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="globalContext">The global context.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc. Also used by debuggers to locate the original source file.</param>
        /// <returns>
        /// A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoString(
            string code,
            Table globalContext = null,
            string codeFriendlyName = null
        )
        {
            if (TryExecuteCachedString(code, globalContext, codeFriendlyName, out DynValue result))
            {
                return result;
            }

            DynValue func = LoadString(code, globalContext, codeFriendlyName);
            return Call(func);
        }

        private bool TryExecuteCachedString(
            string code,
            Table globalContext,
            string codeFriendlyName,
            out DynValue result
        )
        {
            result = null;

            if (_compilationCache == null)
            {
                return false;
            }

            if (code == null)
            {
                throw new ArgumentNullException(nameof(code));
            }

            this.CheckScriptOwnership(globalContext);

            if (code.StartsWith(StringModule.Base64DumpHeader, StringComparison.Ordinal))
            {
                return false;
            }

            if (
                !_compilationCache.TryGet(
                    code,
                    Options.CompatibilityVersion,
                    codeFriendlyName,
                    out Execution.CachedChunk cached
                )
            )
            {
                return false;
            }

            Table environment = globalContext ?? _globalTable;
            ClosureContext closureScope = new(DynValue.NewTable(environment));

            try
            {
                result = _mainProcessor.CallChunk(cached._entryPointAddress, closureScope);
                return true;
            }
            catch (InterpreterException ex)
            {
                ex.AppendCompatibilityContext(this);
                throw;
            }
        }

        /// <summary>
        /// Loads and executes a stream containing a Lua/NovaSharp script.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="globalContext">The global context.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc. Also used by debuggers to locate the original source file.</param>
        /// <returns>
        /// A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoStream(
            Stream stream,
            Table globalContext = null,
            string codeFriendlyName = null
        )
        {
            DynValue func = LoadStream(stream, globalContext, codeFriendlyName);
            return Call(func);
        }

        /// <summary>
        /// Loads and executes a file containing a Lua/NovaSharp script.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="globalContext">The global context.</param>
        /// <param name="codeFriendlyName">Name of the code - used to report errors, etc. Also used by debuggers to locate the original source file.</param>
        /// <returns>
        /// A DynValue containing the result of the processing of the loaded chunk.
        /// </returns>
        public DynValue DoFile(
            string filename,
            Table globalContext = null,
            string codeFriendlyName = null
        )
        {
            object code = LoadFileContent(filename, globalContext, out string resolvedFilename);
            string chunkName = codeFriendlyName ?? resolvedFilename;

            if (code is string source)
            {
                if (TryExecuteCachedString(source, globalContext, chunkName, out DynValue result))
                {
                    return result;
                }

                DynValue stringFunc = LoadStringSkippingCompilationCacheLookup(
                    source,
                    globalContext,
                    chunkName
                );
                return Call(stringFunc);
            }

            DynValue func = LoadFileContent(code, globalContext, chunkName);
            return Call(func);
        }

        /// <summary>
        /// Runs the specified file with all possible defaults for quick experimenting.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// A DynValue containing the result of the processing of the executed script.
        public static DynValue RunFile(string filename)
        {
            Script s = new();
            return s.DoFile(filename);
        }

        /// <summary>
        /// Runs the specified code with all possible defaults for quick experimenting.
        /// </summary>
        /// <param name="code">The Lua/NovaSharp code.</param>
        /// A DynValue containing the result of the processing of the executed script.
        public static DynValue RunString(string code)
        {
            Script s = new();
            return s.DoString(code);
        }

        /// <summary>
        /// Executes an action with compatibility guard - wraps exceptions with compatibility context.
        /// </summary>
        private T ExecuteWithCompatibilityGuard<T>(Func<T> action)
        {
            try
            {
                return action();
            }
            catch (InterpreterException ex)
            {
                ex.AppendCompatibilityContext(this);
                throw;
            }
        }

        /// <summary>
        /// Executes an action with compatibility guard and state - avoids closure allocation.
        /// </summary>
        private TResult ExecuteWithCompatibilityGuard<TState, TResult>(
            TState state,
            Func<TState, TResult> action
        )
        {
            try
            {
                return action(state);
            }
            catch (InterpreterException ex)
            {
                ex.AppendCompatibilityContext(this);
                throw;
            }
        }

        private void ExecuteWithCompatibilityGuard(Action action)
        {
            ExecuteWithCompatibilityGuard(() =>
            {
                action();
                return 0;
            });
        }

        private DynValue MakeClosure(int address, Table envTable = null)
        {
            this.CheckScriptOwnership(envTable);
            Closure c;

            if (envTable == null)
            {
                Instruction meta = _mainProcessor.FindMeta(ref address);

                // if we find the meta for a new chunk, we use the value in the meta for the _ENV upvalue
                if ((meta != null) && (meta.NumVal2 == (int)OpCodeMetadataType.ChunkEntrypoint))
                {
                    c = new Closure(this, address, meta.Value);
                }
                else
                {
                    c = new Closure(
                        this,
                        address,
                        Array.Empty<SymbolRef>(),
                        Array.Empty<DynValue>()
                    );
                }
            }
            else
            {
                c = new Closure(this, address, DynValue.NewTable(envTable));
            }

            return DynValue.NewClosure(c);
        }

        /// <summary>
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);

            if (function.Type == DataType.Function)
            {
                return ExecuteLuaFunctionWithoutArguments(function);
            }

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(this);
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback)
                );
            }

            return CallNonFunction(function);
        }

        /// <summary>
        /// Calls the specified function with one argument.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, DynValue arg)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(this, arg);
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg),
                static state => state._mainProcessor.Call(state.function, state.arg)
            );
        }

        /// <summary>
        /// Calls the specified function with two arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, DynValue arg1, DynValue arg2)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(this, arg1, arg2);
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2),
                static state => state._mainProcessor.Call(state.function, state.arg1, state.arg2)
            );
        }

        /// <summary>
        /// Calls the specified function with three arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, DynValue arg1, DynValue arg2, DynValue arg3)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(this, arg1, arg2, arg3);
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2,
                    arg3
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2, arg3);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3),
                static state =>
                    state._mainProcessor.Call(state.function, state.arg1, state.arg2, state.arg3)
            );
        }

        /// <summary>
        /// Calls the specified function with four arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(this, arg1, arg2, arg3, arg4);
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2,
                    arg3,
                    arg4
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2, arg3, arg4);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4
                    )
            );
        }

        /// <summary>
        /// Calls the specified function with five arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(
                    this,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5
                );
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2, arg3, arg4, arg5);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5
                    )
            );
        }

        /// <summary>
        /// Calls the specified function with six arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(
                    this,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6
                );
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6
                    )
            );
        }

        /// <summary>
        /// Calls the specified function with seven arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <param name="arg7">The seventh argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);
            this.CheckScriptOwnership(arg7);

            if (function.Type == DataType.ClrFunction && function.Callback.HasArgumentViewCallback)
            {
                return function.Callback.InvokeArgumentViewFixed(
                    this,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    arg7
                );
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.InvokeLegacyFixed(
                    CreateDynamicExecutionContext(function.Callback),
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    arg7
                );
            }

            if (function.Type != DataType.Function)
            {
                return CallNonFunction(function, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6,
                        state.arg7
                    )
            );
        }

        /// <summary>
        /// Calls the specified function with caller-owned contiguous arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, ReadOnlySpan<DynValue> args)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(args);

            int maxloops = 10;
            bool isFirstCallMetamethodResolution = true;

            while (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
            {
                if (maxloops <= 0)
                {
                    throw ScriptRuntimeException.LoopInCall();
                }

                DynValue metafunction = _mainProcessor.GetMetamethod(function, Metamethods.Call);

                if (
                    metafunction == null
                    || metafunction.IsNil()
                    || !CanCallMetamethod(metafunction)
                )
                {
                    throw new ArgumentException(
                        "function is not a function and has no __call metamethod."
                    );
                }

                if (
                    isFirstCallMetamethodResolution
                    && TryCallDirectMetamethod(
                        metafunction,
                        function,
                        args,
                        out DynValue directResult
                    )
                )
                {
                    return directResult;
                }

                DynValue[] metaargs = CreateCallMetamethodArguments(function, args);
                function = metafunction;
                args = metaargs;
                isFirstCallMetamethodResolution = false;
                maxloops--;
            }

            if (function.Type == DataType.ClrFunction)
            {
                if (function.Callback.HasArgumentViewCallback)
                {
                    return function.Callback.InvokeArgumentViewSpan(this, args);
                }

                ScriptExecutionContext context = CreateDynamicExecutionContext(function.Callback);
                return function.Callback.InvokeLegacySpan(context, args);
            }

            switch (args.Length)
            {
                case 0:
                    return Call(function);
                case 1:
                    return Call(function, args[0]);
                case 2:
                    return Call(function, args[0], args[1]);
                case 3:
                    return Call(function, args[0], args[1], args[2]);
                case 4:
                    return Call(function, args[0], args[1], args[2], args[3]);
                case 5:
                    return Call(function, args[0], args[1], args[2], args[3], args[4]);
                case 6:
                    return Call(function, args[0], args[1], args[2], args[3], args[4], args[5]);
                case 7:
                    return Call(
                        function,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        args[5],
                        args[6]
                    );
            }

            return ExecuteSpanCallWithCompatibilityGuard(function, args);
        }

        /// <summary>
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, params DynValue[] args)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(args);

            int maxloops = 10;
            bool isFirstCallMetamethodResolution = true;

            while (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
            {
                if (maxloops <= 0)
                {
                    throw ScriptRuntimeException.LoopInCall();
                }

                DynValue metafunction = _mainProcessor.GetMetamethod(function, Metamethods.Call);

                if (
                    metafunction == null
                    || metafunction.IsNil()
                    || !CanCallMetamethod(metafunction)
                )
                {
                    throw new ArgumentException(
                        "function is not a function and has no __call metamethod."
                    );
                }

                if (
                    isFirstCallMetamethodResolution
                    && TryCallDirectMetamethod(
                        metafunction,
                        function,
                        args,
                        out DynValue directResult
                    )
                )
                {
                    return directResult;
                }

                DynValue[] metaargs = new DynValue[args.Length + 1];
                metaargs[0] = function;
                for (int i = 0; i < args.Length; i++)
                {
                    metaargs[i + 1] = args[i];
                }

                function = metafunction;
                args = metaargs;
                isFirstCallMetamethodResolution = false;
                maxloops--;
            }

            if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.Invoke(this, args);
            }

            if (args.Length == 0)
            {
                return ExecuteLuaFunctionWithoutArguments(function);
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, args),
                static state => state._mainProcessor.Call(state.function, state.args)
            );
        }

        private DynValue ExecuteSpanCallWithCompatibilityGuard(
            DynValue function,
            ReadOnlySpan<DynValue> args
        )
        {
            try
            {
                return _mainProcessor.Call(function, args);
            }
            catch (InterpreterException ex)
            {
                ex.AppendCompatibilityContext(this);
                throw;
            }
        }

        private bool TryCallDirectMetamethod(
            DynValue metafunction,
            DynValue self,
            ReadOnlySpan<DynValue> args,
            out DynValue result
        )
        {
            if (!IsDirectCallTarget(metafunction))
            {
                result = null;
                return false;
            }

            switch (args.Length)
            {
                case 0:
                    result = Call(metafunction, self);
                    return true;
                case 1:
                    result = Call(metafunction, self, args[0]);
                    return true;
                case 2:
                    result = Call(metafunction, self, args[0], args[1]);
                    return true;
                case 3:
                    result = Call(metafunction, self, args[0], args[1], args[2]);
                    return true;
                case 4:
                    result = Call(metafunction, self, args[0], args[1], args[2], args[3]);
                    return true;
                case 5:
                    result = CallDirectTarget(
                        metafunction,
                        self,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4]
                    );
                    return true;
                case 6:
                    result = CallDirectTarget(
                        metafunction,
                        self,
                        args[0],
                        args[1],
                        args[2],
                        args[3],
                        args[4],
                        args[5]
                    );
                    return true;
                default:
                    result = null;
                    return false;
            }
        }

        private DynValue CallDirectTarget(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            if (function.Type == DataType.ClrFunction)
            {
                if (function.Callback.HasArgumentViewCallback)
                {
                    return function.Callback.InvokeArgumentViewFixed(
                        this,
                        arg1,
                        arg2,
                        arg3,
                        arg4,
                        arg5,
                        arg6
                    );
                }

                ScriptExecutionContext context = CreateDynamicExecutionContext(function.Callback);
                return function.Callback.InvokeLegacyFixed(
                    context,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6
                );
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6
                    )
            );
        }

        private DynValue CallDirectTarget(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            if (function.Type == DataType.ClrFunction)
            {
                if (function.Callback.HasArgumentViewCallback)
                {
                    return function.Callback.InvokeArgumentViewFixed(
                        this,
                        arg1,
                        arg2,
                        arg3,
                        arg4,
                        arg5,
                        arg6,
                        arg7
                    );
                }

                ScriptExecutionContext context = CreateDynamicExecutionContext(function.Callback);
                return function.Callback.InvokeLegacyFixed(
                    context,
                    arg1,
                    arg2,
                    arg3,
                    arg4,
                    arg5,
                    arg6,
                    arg7
                );
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6,
                        state.arg7
                    )
            );
        }

        /// <summary>
        /// Calls a Lua function with six fixed arguments after the caller has already resolved the call target.
        /// </summary>
        internal DynValue CallDirectLuaFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6
                    )
            );
        }

        /// <summary>
        /// Calls a Lua function with seven fixed arguments after the caller has already resolved the call target.
        /// </summary>
        internal DynValue CallDirectLuaFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            this.CheckScriptOwnership(function);
            this.CheckScriptOwnership(arg1);
            this.CheckScriptOwnership(arg2);
            this.CheckScriptOwnership(arg3);
            this.CheckScriptOwnership(arg4);
            this.CheckScriptOwnership(arg5);
            this.CheckScriptOwnership(arg6);
            this.CheckScriptOwnership(arg7);

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, arg1, arg2, arg3, arg4, arg5, arg6, arg7),
                static state =>
                    state._mainProcessor.Call(
                        state.function,
                        state.arg1,
                        state.arg2,
                        state.arg3,
                        state.arg4,
                        state.arg5,
                        state.arg6,
                        state.arg7
                    )
            );
        }

        private DynValue CallNonFunction(DynValue function)
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, Array.Empty<DynValue>());
            }

            return Call(metafunction, function);
        }

        private DynValue CallNonFunction(DynValue function, DynValue arg)
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg });
            }

            return Call(metafunction, function, arg);
        }

        private DynValue CallNonFunction(DynValue function, DynValue arg1, DynValue arg2)
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg1, arg2);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg1, arg2 });
            }

            return Call(metafunction, function, arg1, arg2);
        }

        private DynValue CallNonFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3
        )
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg1, arg2, arg3);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg1, arg2, arg3 });
            }

            return Call(metafunction, function, arg1, arg2, arg3);
        }

        private DynValue CallNonFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4
        )
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg1, arg2, arg3, arg4);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg1, arg2, arg3, arg4 });
            }

            return Call(metafunction, function, arg1, arg2, arg3, arg4);
        }

        private DynValue CallNonFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5
        )
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg1, arg2, arg3, arg4, arg5);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg1, arg2, arg3, arg4, arg5 });
            }

            return CallDirectTarget(metafunction, function, arg1, arg2, arg3, arg4, arg5);
        }

        private DynValue CallNonFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6
        )
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);
            if (!IsDirectCallTarget(metafunction))
            {
                FixedChainedCallArguments args = new(function, arg1, arg2, arg3, arg4, arg5, arg6);
                if (TryCallChainedNonFunction(metafunction, args, out DynValue result))
                {
                    return result;
                }

                return Call(function, new DynValue[] { arg1, arg2, arg3, arg4, arg5, arg6 });
            }

            return CallDirectTarget(metafunction, function, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        private DynValue CallNonFunction(
            DynValue function,
            DynValue arg1,
            DynValue arg2,
            DynValue arg3,
            DynValue arg4,
            DynValue arg5,
            DynValue arg6,
            DynValue arg7
        )
        {
            DynValue metafunction = GetCallableMetamethodOrThrow(function);

            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                8,
                out DynValue[] arguments
            );
            arguments[0] = function;
            arguments[1] = arg1;
            arguments[2] = arg2;
            arguments[3] = arg3;
            arguments[4] = arg4;
            arguments[5] = arg5;
            arguments[6] = arg6;
            arguments[7] = arg7;

            return Call(metafunction, arguments.AsSpan(0, 8));
        }

        private bool TryCallChainedNonFunction(
            DynValue function,
            FixedChainedCallArguments args,
            out DynValue result
        )
        {
            int maxloops = 9;

            while (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
            {
                if (maxloops <= 0)
                {
                    throw ScriptRuntimeException.LoopInCall();
                }

                DynValue metafunction = GetCallableMetamethodOrThrow(function);
                if (!args.TryPrepend(function, out FixedChainedCallArguments nextArgs))
                {
                    result = null;
                    return false;
                }

                args = nextArgs;
                function = metafunction;
                maxloops--;
            }

            result = CallFixed(function, args);
            return true;
        }

        private DynValue CallFixed(DynValue function, FixedChainedCallArguments args)
        {
            return args.Count switch
            {
                1 => Call(function, args[0]),
                2 => Call(function, args[0], args[1]),
                3 => Call(function, args[0], args[1], args[2]),
                4 => Call(function, args[0], args[1], args[2], args[3]),
                5 => Call(function, args[0], args[1], args[2], args[3], args[4]),
                6 => CallDirectTarget(
                    function,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5]
                ),
                7 => CallDirectTarget(
                    function,
                    args[0],
                    args[1],
                    args[2],
                    args[3],
                    args[4],
                    args[5],
                    args[6]
                ),
                _ => Call(function),
            };
        }

        private static bool IsDirectCallTarget(DynValue function)
        {
            return function.Type == DataType.Function || function.Type == DataType.ClrFunction;
        }

        private DynValue GetCallableMetamethodOrThrow(DynValue function)
        {
            DynValue metafunction = _mainProcessor.GetMetamethod(function, Metamethods.Call);
            if (metafunction != null && !metafunction.IsNil() && CanCallMetamethod(metafunction))
            {
                return metafunction;
            }

            throw new ArgumentException("function is not a function and has no __call metamethod.");
        }

        private static DynValue[] CreateCallMetamethodArguments(
            DynValue function,
            ReadOnlySpan<DynValue> args
        )
        {
            DynValue[] metaargs = new DynValue[args.Length + 1];
            metaargs[0] = function;
            for (int i = 0; i < args.Length; i++)
            {
                metaargs[i + 1] = args[i];
            }

            return metaargs;
        }

        private bool CanCallMetamethod(DynValue metafunction)
        {
            return LuaVersionDefaults.Resolve(Options.CompatibilityVersion)
                    >= LuaCompatibilityVersion.Lua54
                || metafunction.Type == DataType.Function
                || metafunction.Type == DataType.ClrFunction;
        }

        /// <summary>
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, params object[] args)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return CallObjectArguments(function, args.AsSpan());
        }

        /// <summary>
        /// Calls the specified function with caller-owned CLR object argument storage.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(DynValue function, object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return CallObjectArguments(function, args.AsSpan());
        }

        /// <summary>
        /// Calls the specified function with caller-owned contiguous CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(DynValue function, ReadOnlySpan<object> args)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            switch (args.Length)
            {
                case 0:
                    return Call(function);
                case 1:
                    return Call(function, DynValue.FromObject(this, args[0]));
                case 2:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1])
                    );
                case 3:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2])
                    );
                case 4:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3])
                    );
                case 5:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4])
                    );
                case 6:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4]),
                        DynValue.FromObject(this, args[5])
                    );
                case 7:
                    return Call(
                        function,
                        DynValue.FromObject(this, args[0]),
                        DynValue.FromObject(this, args[1]),
                        DynValue.FromObject(this, args[2]),
                        DynValue.FromObject(this, args[3]),
                        DynValue.FromObject(this, args[4]),
                        DynValue.FromObject(this, args[5]),
                        DynValue.FromObject(this, args[6])
                    );
            }

            using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                args.Length,
                out DynValue[] convertedArgs
            );
            for (int i = 0; i < args.Length; i++)
            {
                convertedArgs[i] = DynValue.FromObject(this, args[i]);
            }

            return Call(function, convertedArgs.AsSpan(0, args.Length));
        }

        /// <summary>
        /// Calls the specified function with one CLR object argument.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, object arg)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(function, DynValue.FromObject(this, arg));
        }

        /// <summary>
        /// Calls the specified function with two CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, object arg1, object arg2)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(function, DynValue.FromObject(this, arg1), DynValue.FromObject(this, arg2));
        }

        /// <summary>
        /// Calls the specified function with three CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, object arg1, object arg2, object arg3)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(
                function,
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3)
            );
        }

        /// <summary>
        /// Calls the specified function with four CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(DynValue function, object arg1, object arg2, object arg3, object arg4)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(
                function,
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4)
            );
        }

        /// <summary>
        /// Calls the specified function with five CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(
                function,
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5)
            );
        }

        /// <summary>
        /// Calls the specified function with six CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(
                function,
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5),
                DynValue.FromObject(this, arg6)
            );
        }

        /// <summary>
        /// Calls the specified function with seven CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <param name="arg7">The seventh argument to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            DynValue function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7
        )
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return Call(
                function,
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5),
                DynValue.FromObject(this, arg6),
                DynValue.FromObject(this, arg7)
            );
        }

        /// <summary>
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function)
        {
            return Call(DynValue.FromObject(this, function));
        }

        /// <summary>
        /// Calls the specified function with one CLR object argument.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, object arg)
        {
            return Call(DynValue.FromObject(this, function), DynValue.FromObject(this, arg));
        }

        /// <summary>
        /// Calls the specified function with two CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, object arg1, object arg2)
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2)
            );
        }

        /// <summary>
        /// Calls the specified function with three CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, object arg1, object arg2, object arg3)
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3)
            );
        }

        /// <summary>
        /// Calls the specified function with four CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, object arg1, object arg2, object arg3, object arg4)
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4)
            );
        }

        /// <summary>
        /// Calls the specified function with five CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            object function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5
        )
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5)
            );
        }

        /// <summary>
        /// Calls the specified function with six CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            object function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6
        )
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5),
                DynValue.FromObject(this, arg6)
            );
        }

        /// <summary>
        /// Calls the specified function with seven CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <param name="arg5">The fifth argument to pass to the function.</param>
        /// <param name="arg6">The sixth argument to pass to the function.</param>
        /// <param name="arg7">The seventh argument to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(
            object function,
            object arg1,
            object arg2,
            object arg3,
            object arg4,
            object arg5,
            object arg6,
            object arg7
        )
        {
            return Call(
                DynValue.FromObject(this, function),
                DynValue.FromObject(this, arg1),
                DynValue.FromObject(this, arg2),
                DynValue.FromObject(this, arg3),
                DynValue.FromObject(this, arg4),
                DynValue.FromObject(this, arg5),
                DynValue.FromObject(this, arg6),
                DynValue.FromObject(this, arg7)
            );
        }

        /// <summary>
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, params object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return CallObjectArguments(DynValue.FromObject(this, function), args.AsSpan());
        }

        /// <summary>
        /// Calls the specified function object with caller-owned CLR object argument storage.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(object function, object[] args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            return CallObjectArguments(DynValue.FromObject(this, function), args.AsSpan());
        }

        /// <summary>
        /// Calls the specified function object with caller-owned contiguous CLR object arguments.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called.</param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns>
        /// The return value(s) of the function call.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not callable.</exception>
        public DynValue CallObjectArguments(object function, ReadOnlySpan<object> args)
        {
            return CallObjectArguments(DynValue.FromObject(this, function), args);
        }

        /// <summary>
        /// Creates a coroutine pointing at the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>
        /// The coroutine handle.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function or DataType.ClrFunction</exception>
        public DynValue CreateCoroutine(DynValue function)
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            this.CheckScriptOwnership(function);

            // Check coroutine limit before creating
            CheckCoroutineLimit();

            if (function.Type == DataType.Function)
            {
                return _mainProcessor.CreateCoroutine(function.Function);
            }
            else if (function.Type == DataType.ClrFunction)
            {
                return DynValue.NewCoroutine(new Coroutine(function.Callback));
            }
            else
            {
                throw new ArgumentException(
                    "function is not of DataType.Function or DataType.ClrFunction"
                );
            }
        }

        /// <summary>
        /// Checks whether creating a new coroutine would exceed the configured coroutine limit.
        /// If the limit is exceeded and no callback allows continuation, throws <see cref="Sandboxing.SandboxViolationException"/>.
        /// </summary>
        private void CheckCoroutineLimit()
        {
            Sandboxing.SandboxOptions sandbox = Options.Sandbox;
            if (!sandbox.HasCoroutineLimit || _allocationTracker == null)
            {
                return;
            }

            // Check if we would exceed the limit (current >= max means the next one exceeds)
            if (_allocationTracker.ExceedsCoroutineLimit(sandbox))
            {
                int currentCount = _allocationTracker.CurrentCoroutines;
                int maxCoroutines = sandbox.MaxCoroutines;

                // Invoke callback if set
                Func<Script, int, bool> callback = sandbox.OnCoroutineLimitExceeded;
                if (callback != null && callback(this, currentCount))
                {
                    // Callback returned true - allow continuation
                    return;
                }

                // Throw violation exception
                throw new Sandboxing.SandboxViolationException(
                    Sandboxing.SandboxViolationDetails.CoroutineLimit(
                        maxCoroutines,
                        currentCount + 1
                    )
                );
            }
        }

        /// <summary>
        /// Creates a new coroutine, recycling buffers from a dead coroutine to skip slower buffer creation in Mono.
        /// </summary>
        /// <param name="coroutine">The <see cref="Coroutine"/> to recycle. This coroutine's state must be <see cref="CoroutineState.Dead"/></param>
        /// <param name="function">The function</param>
        /// <returns>
        /// The new coroutine handle.
        /// </returns>
        public DynValue RecycleCoroutine(Coroutine coroutine, DynValue function)
        {
            this.CheckScriptOwnership(coroutine);
            this.CheckScriptOwnership(function);

            if (coroutine == null || coroutine.Type != Coroutine.CoroutineType.Coroutine)
            {
                throw new InvalidOperationException("coroutine is not CoroutineType.Coroutine");
            }

            if (function == null || function.Type != DataType.Function)
            {
                throw new InvalidOperationException("function is not DataType.Function");
            }

            if (coroutine.State != CoroutineState.Dead)
            {
                throw new InvalidOperationException(
                    "coroutine's state must be CoroutineState.Dead to recycle"
                );
            }

            return coroutine.Recycle(_mainProcessor, function.Function);
        }

        /// <summary>
        /// Creates a coroutine pointing at the specified function.
        /// </summary>
        /// <param name="function">The function.</param>
        /// <returns>
        /// The coroutine handle.
        /// </returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function or DataType.ClrFunction</exception>
        public DynValue CreateCoroutine(object function)
        {
            return CreateCoroutine(DynValue.FromObject(this, function));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the debugger is enabled.
        /// Note that unless a debugger attached, this property returns a
        /// value which might not reflect the real status of the debugger.
        /// Use this property if you want to disable the debugger for some
        /// executions.
        /// </summary>
        public bool DebuggerEnabled
        {
            get { return _mainProcessor.DebuggerEnabled; }
            set { _mainProcessor.DebuggerEnabled = value; }
        }

        /// <summary>
        /// Attaches a debugger. This usually should be called by the debugger itself and not by user code.
        /// </summary>
        /// <param name="debugger">The debugger object.</param>
        public void AttachDebugger(IDebugger debugger)
        {
            if (debugger == null)
            {
                throw new ArgumentNullException(nameof(debugger));
            }

            DebuggerEnabled = true;
            _debugger = debugger;
            _mainProcessor.AttachDebugger(debugger);

            foreach (SourceCode src in _sources)
            {
                SignalSourceCodeChange(src);
            }

            SignalByteCodeChange();
        }

        /// <summary>
        /// Gets the source code.
        /// </summary>
        /// <param name="sourceCodeId">The source code identifier.</param>
        /// <returns></returns>
        public SourceCode GetSourceCode(int sourceCodeId)
        {
            return _sources[sourceCodeId];
        }

        /// <summary>
        /// Resolves the first Lua source reference associated with a compiled function.
        /// </summary>
        /// <param name="closure">Closure whose bytecode should be inspected.</param>
        /// <returns>The best available source reference, or <c>null</c> when none is available.</returns>
        internal SourceRef GetFunctionSourceRef(Closure closure)
        {
            if (closure == null)
            {
                return null;
            }

            if (!TryGetFunctionInstructionRange(closure, out int start, out int end))
            {
                return null;
            }

            for (int i = start; i < end; i++)
            {
                SourceRef sourceRef = _byteCode.Code[i].SourceCodeRef;
                if (sourceRef != null)
                {
                    return sourceRef;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves fixed-parameter and vararg metadata for a compiled function.
        /// </summary>
        /// <param name="closure">Closure whose bytecode prologue should be inspected.</param>
        /// <param name="parameterCount">Number of fixed parameters declared by the function.</param>
        /// <param name="isVarArg">Whether the function declares <c>...</c>.</param>
        internal void GetFunctionArgumentInfo(
            Closure closure,
            out int parameterCount,
            out bool isVarArg
        )
        {
            parameterCount = 0;
            isVarArg = false;

            if (
                closure == null
                || !TryGetFunctionInstructionRange(closure, out int start, out int end)
            )
            {
                return;
            }

            int beginFn = FindBeginFn(start, end);
            if (beginFn < 0)
            {
                return;
            }

            int cursor = beginFn + 1;
            if (TryReadArgsInstruction(cursor, end, out parameterCount, out isVarArg))
            {
                return;
            }

            if (IsEnvironmentSetup(cursor, end))
            {
                cursor += 3;
                TryReadArgsInstruction(cursor, end, out parameterCount, out isVarArg);
            }
        }

        private bool TryGetFunctionInstructionRange(Closure closure, out int start, out int end)
        {
            start = closure.EntryPointByteCodeLocation;
            end = _byteCode.Code.Count;

            if ((uint)start >= (uint)_byteCode.Code.Count)
            {
                return false;
            }

            int metaIndex = FindFunctionMetaIndex(start);
            if (metaIndex >= 0)
            {
                Instruction meta = _byteCode.Code[metaIndex];
                start = metaIndex;

                if (meta.NumVal > 0)
                {
                    end = Math.Min(_byteCode.Code.Count, metaIndex + meta.NumVal + 1);
                }
            }

            return true;
        }

        private int FindFunctionMetaIndex(int entryPoint)
        {
            int forward = entryPoint;
            while (forward < _byteCode.Code.Count && _byteCode.Code[forward].OpCode == OpCode.Nop)
            {
                forward++;
            }

            if (forward < _byteCode.Code.Count && IsFunctionMeta(_byteCode.Code[forward]))
            {
                return forward;
            }

            if (entryPoint > 0 && IsFunctionMeta(_byteCode.Code[entryPoint - 1]))
            {
                return entryPoint - 1;
            }

            return -1;
        }

        private static bool IsFunctionMeta(Instruction instruction)
        {
            return instruction.OpCode == OpCode.Meta
                && (
                    instruction.NumVal2 == (int)OpCodeMetadataType.ChunkEntrypoint
                    || instruction.NumVal2 == (int)OpCodeMetadataType.FunctionEntrypoint
                );
        }

        private int FindBeginFn(int start, int end)
        {
            int limit = Math.Min(end, start + 4);
            for (int i = start; i < limit; i++)
            {
                if (_byteCode.Code[i].OpCode == OpCode.BeginFn)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsEnvironmentSetup(int cursor, int end)
        {
            return cursor + 2 < end
                && _byteCode.Code[cursor].OpCode == OpCode.UpValue
                && _byteCode.Code[cursor].Symbol?.NameValue == WellKnownSymbols.ENV
                && _byteCode.Code[cursor + 1].OpCode == OpCode.StoreLcl
                && _byteCode.Code[cursor + 1].Symbol?.NameValue == WellKnownSymbols.ENV
                && _byteCode.Code[cursor + 2].OpCode == OpCode.Pop;
        }

        private bool TryReadArgsInstruction(
            int cursor,
            int end,
            out int parameterCount,
            out bool isVarArg
        )
        {
            parameterCount = 0;
            isVarArg = false;

            if (cursor >= end || _byteCode.Code[cursor].OpCode != OpCode.Args)
            {
                return false;
            }

            SymbolRef[] symbols = _byteCode.Code[cursor].SymbolList;
            if (symbols == null)
            {
                return true;
            }

            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i].NameValue == WellKnownSymbols.VARARGS)
                {
                    isVarArg = true;
                }
                else
                {
                    parameterCount++;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the source code count.
        /// </summary>
        /// <value>
        /// The source code count.
        /// </value>
        public int SourceCodeCount
        {
            get { return _sources.Count; }
        }

        /// <summary>
        /// Loads a module as per the "require" Lua function. http://www.lua.org/pil/8.1.html
        /// </summary>
        /// <param name="modname">The module name</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns></returns>
        /// <exception cref="ScriptRuntimeException">Raised if module is not found</exception>
        public DynValue RequireModule(string modname, Table globalContext = null)
        {
            this.CheckScriptOwnership(globalContext);

            Table globals = globalContext ?? _globalTable;

            WarnIfBit32CompatibilityDisabled(modname);

            // Try to get detailed resolution result with searched paths for better error messages
            IScriptLoader loader = Options.ScriptLoader;
            string filename;
            IReadOnlyList<string> searchedPaths = null;

            if (loader is ScriptLoaderBase baseLoader)
            {
                ModuleResolutionResult result = baseLoader.TryResolveModuleName(modname, globals);
                filename = result.ResolvedPath;
                searchedPaths = result.SearchedPaths;
            }
            else
            {
                // Fallback for custom IScriptLoader implementations
                filename = loader.ResolveModuleName(modname, globals);
            }

            if (filename == null)
            {
                throw new ScriptRuntimeException(
                    FormatModuleNotFoundError(modname, searchedPaths, Options.LuaCompatibleErrors)
                );
            }

            DynValue func = LoadFile(filename, globalContext, filename);
            return func;
        }

        /// <summary>
        /// Formats the "module not found" error message. When <paramref name="luaCompatibleErrors"/>
        /// is enabled, lists all paths that were searched to match reference Lua behavior.
        /// Otherwise, returns a simple message for backward compatibility.
        /// </summary>
        /// <param name="modname">The module name that was not found.</param>
        /// <param name="searchedPaths">The list of paths that were searched.</param>
        /// <param name="luaCompatibleErrors">Whether to include detailed search paths in the error.</param>
        /// <returns>A formatted error message.</returns>
        private static string FormatModuleNotFoundError(
            string modname,
            IReadOnlyList<string> searchedPaths,
            bool luaCompatibleErrors
        )
        {
            // When LuaCompatibleErrors is disabled, return a simple message for backward compatibility
            if (!luaCompatibleErrors)
            {
                return ZString.Concat("module '", modname, "' not found");
            }

            if (searchedPaths == null || searchedPaths.Count == 0)
            {
                using Utf16ValueStringBuilder sb0 = ZString.CreateStringBuilder();
                sb0.Append("module '");
                sb0.Append(modname);
                sb0.Append("' not found:\n\tno field package.preload['");
                sb0.Append(modname);
                sb0.Append("']");
                return sb0.ToString();
            }

            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
            sb.Append("module '");
            sb.Append(modname);
            sb.Append("' not found:\n\tno field package.preload['");
            sb.Append(modname);
            sb.Append("']");

            foreach (string path in searchedPaths)
            {
                sb.Append("\n\tno file '");
                sb.Append(path);
                sb.Append('\'');
            }

            return sb.ToString();
        }

        private void WarnIfBit32CompatibilityDisabled(string moduleName)
        {
            if (
                _bit32CompatibilityWarningEmitted
                || !string.Equals(moduleName, "bit32", StringComparison.Ordinal)
            )
            {
                return;
            }

            LuaCompatibilityProfile profile = CompatibilityProfile;

            if (profile.SupportsBit32Library)
            {
                return;
            }

            using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
            sb.Append(
                "[compatibility] require('bit32') is only available when targeting Lua 5.2. Active profile: "
            );
            sb.Append(profile.DisplayName);
            sb.Append(
                ". Update Script.Options.CompatibilityVersion or ship a custom bit32 module."
            );
            string message = sb.ToString();

            Action<string> sink = Options.DebugPrint ?? Script.GlobalOptions.Platform.DefaultPrint;
            sink(message);

            _bit32CompatibilityWarningEmitted = true;
        }

        /// <summary>
        /// Gets a type metatable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public Table GetTypeMetatable(DataType type)
        {
            int t = (int)type;

            if (t >= 0 && t < _typeMetatables.Length)
            {
                return _typeMetatables[t];
            }

            return null;
        }

        /// <summary>
        /// Sets a type metatable.
        /// </summary>
        /// <param name="type">The type. Must be Nil, Boolean, Number, String or Function</param>
        /// <param name="metatable">The metatable.</param>
        /// <exception cref="System.ArgumentException">Specified type not supported :  + type.ToString()</exception>
        public void SetTypeMetatable(DataType type, Table metatable)
        {
            this.CheckScriptOwnership(metatable);

            int t = (int)type;

            if (t >= 0 && t < _typeMetatables.Length)
            {
                _typeMetatables[t] = metatable;
            }
            else
            {
                throw new ArgumentException("Specified type not supported : " + type.ToString());
            }
        }

        /// <summary>
        /// Warms up the parser/lexer structures so that NovaSharp operations start faster.
        /// </summary>
        public static void WarmUp()
        {
            Script s = new(CoreModules.Basic);
            s.LoadString("return 1;");
        }

        /// <summary>
        /// Creates a new dynamic expression.
        /// </summary>
        /// <param name="code">The code of the expression.</param>
        /// <returns></returns>
        public DynamicExpression CreateDynamicExpression(string code)
        {
            int sourceId = _sources.Count;
            SourceCode source = new(ZString.Concat("__dynamic_", sourceId), code, sourceId, this);
            _sources.Add(source);

            try
            {
                DynamicExprExpression dee = LoaderFast.LoadDynamicExpr(this, source);
                SignalSourceCodeChange(source);
                return new DynamicExpression(this, code, dee);
            }
            catch
            {
                if (_sources.Count > sourceId)
                {
                    _sources.RemoveAt(sourceId);
                }

                throw;
            }
        }

        /// <summary>
        /// Creates a new dynamic expression which is actually quite static, returning always the same constant value.
        /// </summary>
        /// <param name="code">The code of the not-so-dynamic expression.</param>
        /// <param name="constant">The constant to return.</param>
        /// <returns></returns>
        public DynamicExpression CreateConstantDynamicExpression(string code, DynValue constant)
        {
            this.CheckScriptOwnership(constant);

            return new DynamicExpression(this, code, constant);
        }

        /// <summary>
        /// Gets an execution context exposing only partial functionality, which should be used for
        /// those cases where the execution engine is not really running - for example for dynamic expression
        /// or calls from CLR to CLR callbacks
        /// </summary>
        internal ScriptExecutionContext CreateDynamicExecutionContext(CallbackFunction func = null)
        {
            return new ScriptExecutionContext(_mainProcessor, func, null, isDynamic: true);
        }

        /// <summary>
        /// Exposes the main processor for unit tests that need to inspect VM state.
        /// </summary>
        internal Processor GetMainProcessorForTests()
        {
            return _mainProcessor;
        }

        /// <summary>
        /// Exposes the compiled bytecode for unit tests that need to inspect emitted instructions.
        /// </summary>
        internal ByteCode GetByteCodeForTests()
        {
            return _byteCode;
        }

        /// <summary>
        /// NovaSharp (like Lua itself) provides a registry, a predefined table that can be used by any CLR code to
        /// store whatever Lua values it needs to store.
        /// Any CLR code can store data into this table, but it should take care to choose keys
        /// that are different from those used by other libraries, to avoid collisions.
        /// Typically, you should use as key a string GUID, a string containing your library name, or a
        /// userdata with the address of a CLR object in your code.
        /// </summary>
        public Table Registry { get; private set; }

        /// <summary>
        /// Gets a banner string with copyright info, link to website, version, etc.
        /// </summary>
        public static string GetBanner(string subproduct = null)
        {
            subproduct = (subproduct != null) ? (subproduct + " ") : "";

            using Utf16ValueStringBuilder sb = ZStringBuilder.Create();
            sb.AppendLine(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "NovaSharp {0}{1} [{2}]",
                    subproduct,
                    VERSION,
                    GlobalOptions.Platform.GetPlatformName()
                )
            );
            sb.AppendLine("Copyright (C) 2014-2016 Marco Mastropaolo");
            sb.AppendLine("http://www.NovaSharp.org");
            return sb.ToString();
        }

        /// <summary>
        /// Provides the owning script reference for <see cref="IScriptPrivateResource"/> consumers.
        /// </summary>
        public virtual Script OwnerScript => this;

        private static string ResolveFileNameWithLegacyFallback(
            IScriptLoader scriptLoader,
            string filename,
            Table globalContext
        )
        {
            if (scriptLoader == null)
            {
                throw new ArgumentNullException(nameof(scriptLoader));
            }

            if (scriptLoader is ScriptLoaderBase scriptLoaderBase)
            {
                return scriptLoaderBase.ResolveFileName(filename, globalContext);
            }

            MethodInfo resolveFileName = LegacyResolveFileNameMethods.GetOrAdd(
                scriptLoader.GetType(),
                static type =>
                {
                    InterfaceMapping mapping = type.GetInterfaceMap(typeof(IScriptLoader));

                    for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
                    {
                        if (
                            mapping.InterfaceMethods[i].Name
                            == nameof(IScriptLoader.ResolveFileName)
                        )
                        {
                            return mapping.TargetMethods[i];
                        }
                    }

                    return null;
                }
            );

            if (resolveFileName == null)
            {
                return filename;
            }

            return (string)
                resolveFileName.Invoke(scriptLoader, new object[] { filename, globalContext });
        }

        private sealed class GlobalOptionsScope : IDisposable
        {
            public GlobalOptionsScope(ScriptGlobalOptions options, GlobalOptionsScope previousScope)
            {
                Options = options ?? throw new ArgumentNullException(nameof(options));
                PreviousScope = previousScope;
            }

            /// <summary>
            /// Gets or sets the options snapshot managed by this scope.
            /// </summary>
            public ScriptGlobalOptions Options { get; set; }

            private GlobalOptionsScope PreviousScope { get; }

            /// <summary>
            /// Restores the previously active scope when the current scope is disposed.
            /// </summary>
            public void Dispose()
            {
                ScopedGlobalOptions.Value = PreviousScope;
            }
        }

        private sealed class DefaultOptionsScope : IDisposable
        {
            private readonly ScriptOptions _snapshot;
            private bool _disposed;

            public DefaultOptionsScope(ScriptOptions defaults)
            {
                if (defaults == null)
                {
                    throw new ArgumentNullException(nameof(defaults));
                }

                _snapshot = new ScriptOptions(defaults);
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                ScriptOptions defaultOptions = DefaultOptions;

                defaultOptions.ScriptLoader = _snapshot.ScriptLoader;
                defaultOptions.DebugPrint = _snapshot.DebugPrint;
                defaultOptions.DebugInput = _snapshot.DebugInput;
                defaultOptions.UseLuaErrorLocations = _snapshot.UseLuaErrorLocations;
                defaultOptions.ColonOperatorClrCallbackBehaviour =
                    _snapshot.ColonOperatorClrCallbackBehaviour;
                defaultOptions.Stdin = _snapshot.Stdin;
                defaultOptions.Stdout = _snapshot.Stdout;
                defaultOptions.Stderr = _snapshot.Stderr;
                defaultOptions.TailCallOptimizationThreshold =
                    _snapshot.TailCallOptimizationThreshold;
                defaultOptions.CheckThreadAccess = _snapshot.CheckThreadAccess;
                defaultOptions.CompatibilityVersion = _snapshot.CompatibilityVersion;
                defaultOptions.HighResolutionClock = _snapshot.HighResolutionClock;
                defaultOptions.TimeProvider = _snapshot.TimeProvider;
            }
        }
    }
}
