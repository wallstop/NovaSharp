namespace NovaSharp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataStructs;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Sandboxing;

    /// <summary>
    /// Small public facade over the current NovaSharp VM.
    /// </summary>
    public sealed class LuaEngine : IDisposable
    {
        private readonly Script _script;
        private readonly LuaTable _globals;

        private LuaEngine(LuaEngineOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.EnableScriptCaching && options.ScriptCacheMaxEntries < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options.ScriptCacheMaxEntries,
                    "LuaEngineOptions.ScriptCacheMaxEntries cannot be negative when script caching is enabled."
                );
            }

            ScriptOptions scriptOptions = CreateScriptOptions(options, this);
            _script = new Script(ToCoreModules(options.Modules), scriptOptions);
            _globals = new LuaTable(_script, _script.Globals);
        }

        /// <summary>
        /// Creates an engine with default options.
        /// </summary>
        public static LuaEngine Create()
        {
            return Create(LuaEngineOptions.Default);
        }

        /// <summary>
        /// Creates an engine with the provided options.
        /// </summary>
        public static LuaEngine Create(LuaEngineOptions options)
        {
            return new LuaEngine(options);
        }

        /// <summary>
        /// Gets the global table.
        /// </summary>
        public LuaTable Globals
        {
            get
            {
                ThrowIfDisposed();
                return _globals;
            }
        }

        /// <summary>
        /// Runs a Lua chunk and returns its first result.
        /// </summary>
        public LuaValue Run(string code, string chunkName = null)
        {
            ThrowIfDisposed();
            try
            {
                return WrapResult(_script.DoString(code, null, chunkName));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Asynchronous placeholder for the future coroutine/host-await bridge.
        /// </summary>
        public ValueTask<LuaValue> RunAsync(
            string code,
            string chunkName = null,
            CancellationToken cancellationToken = default
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<LuaValue>(Run(code, chunkName));
        }

        /// <summary>
        /// Compiles a Lua chunk for repeated execution.
        /// </summary>
        public LuaChunk Compile(string code, string chunkName = null)
        {
            ThrowIfDisposed();
            try
            {
                return new LuaChunk(_script, _script.CompileString(code, null, chunkName));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls a Lua function with no arguments.
        /// </summary>
        public LuaValue Call(LuaFunction function)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                return WrapResult(_script.Call(function.ToDynValue(_script)));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls a Lua function with one argument.
        /// </summary>
        public LuaValue Call(LuaFunction function, LuaValue arg0)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                return WrapResult(
                    _script.Call(
                        function.ToDynValue(_script),
                        arg0.ToDynValueAfterOwnerChecked(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls a Lua function with two arguments.
        /// </summary>
        public LuaValue Call(LuaFunction function, LuaValue arg0, LuaValue arg1)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                return WrapResult(
                    _script.Call(
                        function.ToDynValue(_script),
                        arg0.ToDynValueAfterOwnerChecked(_script),
                        arg1.ToDynValueAfterOwnerChecked(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls a Lua function with three arguments.
        /// </summary>
        public LuaValue Call(LuaFunction function, LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                return WrapResult(
                    _script.Call(
                        function.ToDynValue(_script),
                        arg0.ToDynValueAfterOwnerChecked(_script),
                        arg1.ToDynValueAfterOwnerChecked(_script),
                        arg2.ToDynValueAfterOwnerChecked(_script)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls a Lua function with caller-owned contiguous arguments.
        /// </summary>
        public LuaValue Call(LuaFunction function, ReadOnlySpan<LuaValue> args)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                DynValue functionValue = function.ToDynValue(_script);
                switch (args.Length)
                {
                    case 0:
                        return WrapResult(_script.Call(functionValue));
                    case 1:
                        return WrapResult(
                            _script.Call(
                                functionValue,
                                args[0].ToDynValueAfterOwnerChecked(_script)
                            )
                        );
                    case 2:
                        return WrapResult(
                            _script.Call(
                                functionValue,
                                args[0].ToDynValueAfterOwnerChecked(_script),
                                args[1].ToDynValueAfterOwnerChecked(_script)
                            )
                        );
                    case 3:
                        return WrapResult(
                            _script.Call(
                                functionValue,
                                args[0].ToDynValueAfterOwnerChecked(_script),
                                args[1].ToDynValueAfterOwnerChecked(_script),
                                args[2].ToDynValueAfterOwnerChecked(_script)
                            )
                        );
                }

                using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                    args.Length,
                    out DynValue[] converted
                );
                for (int i = 0; i < args.Length; i++)
                {
                    converted[i] = args[i].ToDynValueAfterOwnerChecked(_script);
                }

                return WrapResult(_script.Call(functionValue, converted.AsSpan(0, args.Length)));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Creates an empty Lua table. Capacity arguments are reserved for the table rewrite.
        /// </summary>
        [SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Tables are engine-owned and must capture this engine."
        )]
        public LuaTable CreateTable(int arrayCapacity = 0, int hashCapacity = 0)
        {
            ThrowIfDisposed();
            if (arrayCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayCapacity));
            }

            if (hashCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hashCapacity));
            }

            return new LuaTable(_script, new Table(_script));
        }

        /// <summary>
        /// Creates a Lua-callable function from a host callback.
        /// </summary>
        public LuaValue CreateCallback(LuaCallback callback, string name = null)
        {
            ThrowIfDisposed();
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            CallbackFunction function = CallbackFunction.FromArgumentView(
                _script,
                args => InvokeCallback(callback, args),
                name
            );
            return Wrap(DynValue.FromCallback(function));
        }

        /// <summary>
        /// Creates a coroutine from a Lua function.
        /// </summary>
        public LuaCoroutine CreateCoroutine(LuaFunction function)
        {
            ThrowIfDisposed();
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            try
            {
                DynValue value = _script.CreateCoroutine(function.ToDynValue(_script));
                return new LuaCoroutine(_script, value);
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Explicitly trims process-wide NovaSharp-owned shared pools and this engine's reclaimable cache metadata.
        /// </summary>
        public void TrimMemory(LuaMemoryTrimLevel level)
        {
            ThrowIfDisposed();
            _script.TrimMemory(ToPoolTrimLevel(level));
        }

        /// <summary>
        /// Gets approximate retained-memory statistics for this engine plus process-wide NovaSharp shared pools.
        /// </summary>
        /// <remarks>
        /// Statistics include this engine's script-lifetime metadata and VM stacks together with
        /// static shared pools used by all engines in the current process. Arrays delegated to
        /// <see cref="System.Buffers.ArrayPool{T}.Shared"/> are intentionally opaque and are not
        /// counted as retained NovaSharp-owned pool entries.
        /// </remarks>
        public LuaMemoryStatistics GetMemoryStatistics()
        {
            ThrowIfDisposed();
            return _script.GetMemoryStatistics();
        }

        /// <summary>
        /// Disposes the facade and invalidates handles created by it.
        /// </summary>
        public void Dispose()
        {
            _script.InvalidateFacadeLifetime();
        }

        /// <summary>
        /// Wraps a VM value as an engine-owned facade value.
        /// </summary>
        internal LuaValue Wrap(DynValue value)
        {
            ThrowIfDisposed();
            return LuaValue.Wrap(_script, value);
        }

        /// <summary>
        /// Wraps the first scalar VM result as an engine-owned facade value.
        /// </summary>
        internal LuaValue WrapResult(DynValue value)
        {
            ThrowIfDisposed();
            return LuaValue.WrapResult(_script, value);
        }

        private DynValue InvokeCallback(LuaCallback callback, CallbackArgumentsView args)
        {
            try
            {
                int count = args.Count;
                if (count == 0)
                {
                    return callback(new LuaContext(this), ReadOnlySpan<LuaValue>.Empty)
                        .ToDynValue(_script);
                }

                using PooledResource<LuaValue[]> pooled = SystemArrayPool<LuaValue>.Get(
                    count,
                    out LuaValue[] values
                );
                for (int i = 0; i < count; i++)
                {
                    values[i] = Wrap(args[i]);
                }

                return callback(new LuaContext(this), new ReadOnlySpan<LuaValue>(values, 0, count))
                    .ToDynValue(_script);
            }
            catch (InterpreterException)
            {
                throw;
            }
            catch (Exception exception)
                when (exception is ArgumentException
                    || exception is InvalidOperationException
                    || exception is ArithmeticException
                    || exception is FormatException
                    || exception is NotSupportedException
                )
            {
                throw new ScriptRuntimeException(exception);
            }
        }

        /// <summary>
        /// Throws when this engine has been disposed.
        /// </summary>
        internal void ThrowIfDisposed()
        {
            _script.ThrowIfDisposed();
        }

        /// <summary>
        /// Gets the VM script backing this facade.
        /// </summary>
        internal Script Script => _script;

        /// <summary>
        /// Ensures an engine-owned handle is being used with the engine that created it.
        /// </summary>
        internal static void EnsureSameOwner(Script ownerScript, Script expectedOwnerScript)
        {
            if (!ReferenceEquals(ownerScript, expectedOwnerScript))
            {
                throw new InvalidOperationException(
                    "Lua handle belongs to a different LuaEngine instance."
                );
            }
        }

        private static ScriptOptions CreateScriptOptions(LuaEngineOptions options, LuaEngine owner)
        {
            ScriptOptions scriptOptions = new ScriptOptions(Script.DefaultOptions)
            {
                CompatibilityVersion = ToCompatibilityVersion(options.Version),
                Sandbox =
                    options.Sandbox == null
                        ? SandboxOptions.Unrestricted
                        : options.Sandbox.ToSandboxOptions(),
                EnableScriptCaching = options.EnableScriptCaching,
                ScriptCacheMaxEntries = options.ScriptCacheMaxEntries,
            };

            if (options.Loader != null)
            {
                scriptOptions.ScriptLoader = new LuaScriptLoaderAdapter(owner, options.Loader);
            }

            if (options.Time != null)
            {
                scriptOptions.TimeProvider = new LuaTimeProviderAdapter(options.Time);
            }

            if (options.Random != null)
            {
                scriptOptions.RandomProvider = new LuaRandomProviderAdapter(options.Random);
            }

            if (options.Print != null)
            {
                scriptOptions.DebugPrint = options.Print;
            }

            return scriptOptions;
        }

        private static LuaCompatibilityVersion ToCompatibilityVersion(LuaVersion version)
        {
            switch (version)
            {
                case LuaVersion.Latest:
                    return LuaCompatibilityVersion.Latest;
                case LuaVersion.Lua55:
                    return LuaCompatibilityVersion.Lua55;
                case LuaVersion.Lua54:
                    return LuaCompatibilityVersion.Lua54;
                case LuaVersion.Lua53:
                    return LuaCompatibilityVersion.Lua53;
                case LuaVersion.Lua52:
                    return LuaCompatibilityVersion.Lua52;
                case LuaVersion.Lua51:
                    return LuaCompatibilityVersion.Lua51;
                default:
                    throw new ArgumentOutOfRangeException(nameof(version));
            }
        }

        private static PoolTrimLevel ToPoolTrimLevel(LuaMemoryTrimLevel level)
        {
            switch (level)
            {
                case LuaMemoryTrimLevel.Idle:
                    return PoolTrimLevel.Idle;
                case LuaMemoryTrimLevel.MemoryPressure:
                    return PoolTrimLevel.MemoryPressure;
                case LuaMemoryTrimLevel.Critical:
                    return PoolTrimLevel.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        private static CoreModules ToCoreModules(LuaCoreModules modules)
        {
            return (CoreModules)modules;
        }
    }
}
