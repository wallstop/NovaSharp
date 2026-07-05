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
        private bool _disposed;

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
            _globals = new LuaTable(this, _script.Globals);
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
                return new LuaChunk(this, _script.CompileString(code, null, chunkName));
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
                return WrapResult(_script.Call(function.ToDynValue(this)));
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
                    _script.Call(function.ToDynValue(this), arg0.ToDynValueAfterOwnerChecked(this))
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
                        function.ToDynValue(this),
                        arg0.ToDynValueAfterOwnerChecked(this),
                        arg1.ToDynValueAfterOwnerChecked(this)
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
                        function.ToDynValue(this),
                        arg0.ToDynValueAfterOwnerChecked(this),
                        arg1.ToDynValueAfterOwnerChecked(this),
                        arg2.ToDynValueAfterOwnerChecked(this)
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
                DynValue functionValue = function.ToDynValue(this);
                switch (args.Length)
                {
                    case 0:
                        return WrapResult(_script.Call(functionValue));
                    case 1:
                        return WrapResult(
                            _script.Call(functionValue, args[0].ToDynValueAfterOwnerChecked(this))
                        );
                    case 2:
                        return WrapResult(
                            _script.Call(
                                functionValue,
                                args[0].ToDynValueAfterOwnerChecked(this),
                                args[1].ToDynValueAfterOwnerChecked(this)
                            )
                        );
                    case 3:
                        return WrapResult(
                            _script.Call(
                                functionValue,
                                args[0].ToDynValueAfterOwnerChecked(this),
                                args[1].ToDynValueAfterOwnerChecked(this),
                                args[2].ToDynValueAfterOwnerChecked(this)
                            )
                        );
                }

                using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                    args.Length,
                    out DynValue[] converted
                );
                for (int i = 0; i < args.Length; i++)
                {
                    converted[i] = args[i].ToDynValueAfterOwnerChecked(this);
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

            return new LuaTable(this, new Table(_script));
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
                DynValue value = _script.CreateCoroutine(function.ToDynValue(this));
                return new LuaCoroutine(this, value);
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Disposes the facade and invalidates handles created by it.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// Wraps a VM value as an engine-owned facade value.
        /// </summary>
        internal LuaValue Wrap(DynValue value)
        {
            ThrowIfDisposed();
            DynValue wrapped = value ?? DynValue.Nil;
            LuaEngine owner = LuaValue.RequiresOwner(wrapped) ? this : null;
            return new LuaValue(owner, wrapped);
        }

        /// <summary>
        /// Wraps the first scalar VM result as an engine-owned facade value.
        /// </summary>
        internal LuaValue WrapResult(DynValue value)
        {
            DynValue scalar = (value ?? DynValue.Nil).ToScalar();
            return Wrap(scalar.Type == DataType.Void ? DynValue.Nil : scalar);
        }

        /// <summary>
        /// Calls an already owner-validated Lua function with no arguments.
        /// </summary>
        internal LuaValue CallOwned(DynValue function)
        {
            ThrowIfDisposed();
            try
            {
                return WrapResult(_script.Call(function));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls an already owner-validated Lua function with one argument.
        /// </summary>
        internal LuaValue CallOwned(DynValue function, LuaValue arg0)
        {
            ThrowIfDisposed();
            try
            {
                return WrapResult(_script.Call(function, arg0.ToDynValueAfterOwnerChecked(this)));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls an already owner-validated Lua function with two arguments.
        /// </summary>
        internal LuaValue CallOwned(DynValue function, LuaValue arg0, LuaValue arg1)
        {
            ThrowIfDisposed();
            try
            {
                return WrapResult(
                    _script.Call(
                        function,
                        arg0.ToDynValueAfterOwnerChecked(this),
                        arg1.ToDynValueAfterOwnerChecked(this)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls an already owner-validated Lua function with three arguments.
        /// </summary>
        internal LuaValue CallOwned(DynValue function, LuaValue arg0, LuaValue arg1, LuaValue arg2)
        {
            ThrowIfDisposed();
            try
            {
                return WrapResult(
                    _script.Call(
                        function,
                        arg0.ToDynValueAfterOwnerChecked(this),
                        arg1.ToDynValueAfterOwnerChecked(this),
                        arg2.ToDynValueAfterOwnerChecked(this)
                    )
                );
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        /// <summary>
        /// Calls an already owner-validated Lua function with caller-owned contiguous arguments.
        /// </summary>
        internal LuaValue CallOwned(DynValue function, ReadOnlySpan<LuaValue> args)
        {
            ThrowIfDisposed();
            try
            {
                switch (args.Length)
                {
                    case 0:
                        return WrapResult(_script.Call(function));
                    case 1:
                        return WrapResult(
                            _script.Call(function, args[0].ToDynValueAfterOwnerChecked(this))
                        );
                    case 2:
                        return WrapResult(
                            _script.Call(
                                function,
                                args[0].ToDynValueAfterOwnerChecked(this),
                                args[1].ToDynValueAfterOwnerChecked(this)
                            )
                        );
                    case 3:
                        return WrapResult(
                            _script.Call(
                                function,
                                args[0].ToDynValueAfterOwnerChecked(this),
                                args[1].ToDynValueAfterOwnerChecked(this),
                                args[2].ToDynValueAfterOwnerChecked(this)
                            )
                        );
                }

                using PooledResource<DynValue[]> pooled = DynValueArrayPool.Get(
                    args.Length,
                    out DynValue[] converted
                );
                for (int i = 0; i < args.Length; i++)
                {
                    converted[i] = args[i].ToDynValueAfterOwnerChecked(this);
                }

                return WrapResult(_script.Call(function, converted.AsSpan(0, args.Length)));
            }
            catch (InterpreterException exception)
            {
                throw LuaException.Wrap(exception);
            }
        }

        private DynValue InvokeCallback(LuaCallback callback, CallbackArgumentsView args)
        {
            try
            {
                int count = args.Count;
                if (count == 0)
                {
                    return callback(new LuaContext(this), ReadOnlySpan<LuaValue>.Empty)
                        .ToDynValue(this);
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
                    .ToDynValue(this);
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
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LuaEngine));
            }
        }

        /// <summary>
        /// Ensures an engine-owned handle is being used with the engine that created it.
        /// </summary>
        internal static void EnsureSameOwner(LuaEngine owner, LuaEngine expectedOwner)
        {
            if (!ReferenceEquals(owner, expectedOwner))
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

        private static CoreModules ToCoreModules(LuaCoreModules modules)
        {
            return (CoreModules)modules;
        }
    }
}
