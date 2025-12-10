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
        private bool _bit32CompatibilityWarningEmitted;
        private static ScriptGlobalOptions GlobalOptionsSnapshot;
        private static readonly AsyncLocal<GlobalOptionsScope> ScopedGlobalOptions = new();
        private static readonly ConcurrentDictionary<
            Type,
            MethodInfo
        > LegacyResolveFileNameMethods = new();

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
        /// Initializes a new instance of the <see cref="Script"/> clas.s
        /// </summary>
        public Script()
            : this(CoreModulePresets.Default, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Script"/> class.
        /// </summary>
        /// <param name="coreModules">The core modules to be pre-registered in the default global table.</param>
        public Script(CoreModules coreModules)
            : this(coreModules, null) { }

        /// <summary>
        /// Initializes a new instance using a custom options snapshot.
        /// </summary>
        public Script(ScriptOptions options)
            : this(CoreModulePresets.Default, options) { }

        /// <summary>
        /// Initializes a new instance with modules + options.
        /// </summary>
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

            string chunkName =
                $"libfunc_{funcFriendlyName ?? _sources.Count.ToString(CultureInfo.InvariantCulture)}";

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
            return ExecuteWithCompatibilityGuard(() =>
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

                string chunkName =
                    $"{codeFriendlyName ?? "chunk_" + _sources.Count.ToString(CultureInfo.InvariantCulture)}";

                SourceCode source = new(codeFriendlyName ?? chunkName, code, _sources.Count, this);

                _sources.Add(source);

                int address = LoaderFast.LoadChunk(this, source, _byteCode);

                SignalSourceCodeChange(source);
                SignalByteCodeChange();

                return MakeClosure(address, globalTable ?? _globalTable);
            });
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
            return ExecuteWithCompatibilityGuard(() =>
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
                else
                {
                    string chunkName =
                        $"{codeFriendlyName ?? "dump_" + _sources.Count.ToString(CultureInfo.InvariantCulture)}";

                    SourceCode source = new(
                        codeFriendlyName ?? chunkName,
                        $"-- This script was decoded from a binary dump - dump_{_sources.Count}",
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
                    else
                    {
                        return MakeClosure(address);
                    }
                }
            });
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
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            this.CheckScriptOwnership(globalContext);

            Table globals = globalContext ?? _globalTable;

            filename = ResolveFileNameWithLegacyFallback(Options.ScriptLoader, filename, globals);

            object code = Options.ScriptLoader.LoadFile(filename, globals);

            if (code is string s)
            {
                return LoadString(s, globalContext, friendlyFilename ?? filename);
            }
            else if (code is byte[] bytes)
            {
                using MemoryStream ms = new(bytes);
                return LoadStream(ms, globalContext, friendlyFilename ?? filename);
            }
            else if (code is Stream stream)
            {
                try
                {
                    return LoadStream(stream, globalContext, friendlyFilename ?? filename);
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
                        $"Unsupported return type from IScriptLoader.LoadFile : {code.GetType()}"
                    );
                }
            }
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
            DynValue func = LoadString(code, globalContext, codeFriendlyName);
            return Call(func);
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
            DynValue func = LoadFile(filename, globalContext, codeFriendlyName);
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
                    c = new Closure(
                        this,
                        address,
                        new SymbolRef[] { SymbolRef.UpValue(WellKnownSymbols.ENV, 0) },
                        new DynValue[] { meta.Value }
                    );
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
                SymbolRef[] syms = new SymbolRef[]
                {
                    new()
                    {
                        EnvironmentRef = null,
                        IndexValue = 0,
                        NameValue = WellKnownSymbols.ENV,
                        SymbolType = SymbolRefType.DefaultEnv,
                    },
                };

                DynValue[] vals = new DynValue[] { DynValue.NewTable(envTable) };

                c = new Closure(this, address, syms, vals);
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
            return Call(function, Array.Empty<DynValue>());
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
            return Call(function, new DynValue[] { arg });
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
            return Call(function, new DynValue[] { arg1, arg2 });
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
            return Call(function, new DynValue[] { arg1, arg2, arg3 });
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

            if (function.Type != DataType.Function && function.Type != DataType.ClrFunction)
            {
                DynValue metafunction = _mainProcessor.GetMetamethod(function, "__call");

                if (metafunction != null)
                {
                    DynValue[] metaargs = new DynValue[args.Length + 1];
                    metaargs[0] = function;
                    for (int i = 0; i < args.Length; i++)
                    {
                        metaargs[i + 1] = args[i];
                    }

                    function = metafunction;
                    args = metaargs;
                }
                else
                {
                    throw new ArgumentException(
                        "function is not a function and has no __call metamethod."
                    );
                }
            }
            else if (function.Type == DataType.ClrFunction)
            {
                return function.Callback.ClrCallback(
                    CreateDynamicExecutionContext(function.Callback),
                    new CallbackArguments(args, false)
                );
            }

            return ExecuteWithCompatibilityGuard(
                (_mainProcessor, function, args),
                static state => state._mainProcessor.Call(state.function, state.args)
            );
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

            DynValue[] dargs = new DynValue[args.Length];

            for (int i = 0; i < dargs.Length; i++)
            {
                dargs[i] = DynValue.FromObject(this, args[i]);
            }

            return Call(function, dargs);
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
        /// Calls the specified function.
        /// </summary>
        /// <param name="function">The Lua/NovaSharp function to be called </param>
        /// <param name="args">The arguments to pass to the function.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">Thrown if function is not of DataType.Function</exception>
        public DynValue Call(object function, params object[] args)
        {
            return Call(DynValue.FromObject(this, function), args);
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

            string filename = Options.ScriptLoader.ResolveModuleName(modname, globals);

            if (filename == null)
            {
                throw new ScriptRuntimeException("module '{0}' not found", modname);
            }

            DynValue func = LoadFile(filename, globalContext, filename);
            return func;
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

            string message =
                $"[compatibility] require('bit32') is only available when targeting Lua 5.2. Active profile: {profile.DisplayName}. Update Script.Options.CompatibilityVersion or ship a custom bit32 module.";

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
            SourceCode source = new($"__dynamic_{sourceId}", code, sourceId, this);
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
