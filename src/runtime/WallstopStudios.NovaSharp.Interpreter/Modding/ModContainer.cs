namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Cysharp.Text;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Provides an isolated container for loading, executing, and managing Lua mods
    /// with lifecycle hooks (load, unload, reload).
    /// </summary>
    public class ModContainer : IModContainer
    {
        private readonly object _stateLock = new object();
        private readonly List<string> _entryPoints;
        private Script _script;
        private ModLoadState _state;
        private Exception _lastError;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModContainer"/> class.
        /// </summary>
        /// <param name="modId">The unique identifier for this mod.</param>
        /// <param name="displayName">The display name of the mod. If null, uses <paramref name="modId"/>.</param>
        public ModContainer(string modId, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(modId))
            {
                throw new ArgumentException("Mod ID cannot be null or empty.", nameof(modId));
            }

            ModId = modId;
            DisplayName = displayName ?? modId;
            _entryPoints = new List<string>();
            _state = ModLoadState.Unloaded;
            CoreModules = CoreModulePresets.Default;
        }

        /// <inheritdoc/>
        public string ModId { get; }

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <inheritdoc/>
        public ModLoadState State
        {
            get
            {
                lock (_stateLock)
                {
                    return _state;
                }
            }
        }

        /// <inheritdoc/>
        public Script Script
        {
            get
            {
                lock (_stateLock)
                {
                    return _script;
                }
            }
        }

        /// <inheritdoc/>
        public Table Globals
        {
            get
            {
                lock (_stateLock)
                {
                    return _script?.Globals;
                }
            }
        }

        /// <inheritdoc/>
        public Exception LastError
        {
            get
            {
                lock (_stateLock)
                {
                    return _lastError;
                }
            }
        }

        /// <inheritdoc/>
        public ScriptOptions ScriptOptions { get; set; }

        /// <inheritdoc/>
        public CoreModules CoreModules { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked to create the <see cref="Script"/> instance.
        /// If <c>null</c>, uses the default constructor with <see cref="ScriptOptions"/> and <see cref="CoreModules"/>.
        /// </summary>
        public Func<ModContainer, Script> ScriptFactory { get; set; }

        /// <summary>
        /// Gets or sets an action invoked to configure the <see cref="Script"/> after creation but before entry points execute.
        /// </summary>
        public Action<ModContainer, Script> ScriptConfigurator { get; set; }

        /// <summary>
        /// Gets or sets an action invoked when the mod is about to be unloaded, allowing cleanup in Lua.
        /// </summary>
        public Action<ModContainer, Script> UnloadHandler { get; set; }

        /// <summary>
        /// Gets the list of entry point scripts to execute when loading the mod.
        /// Entry points are executed in order.
        /// </summary>
        public IReadOnlyList<string> EntryPoints => _entryPoints;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnLoading;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnLoaded;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnUnloading;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnUnloaded;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnReloading;

        /// <inheritdoc/>
        public event EventHandler<ModEventArgs> OnReloaded;

        /// <inheritdoc/>
        public event EventHandler<ModErrorEventArgs> OnError;

        /// <summary>
        /// Adds an entry point script to be executed when the mod loads.
        /// </summary>
        /// <param name="scriptCode">The Lua code to execute.</param>
        /// <returns>This container for method chaining.</returns>
        public ModContainer AddEntryPoint(string scriptCode)
        {
            if (string.IsNullOrEmpty(scriptCode))
            {
                throw new ArgumentException(
                    "Entry point script cannot be null or empty.",
                    nameof(scriptCode)
                );
            }

            _entryPoints.Add(scriptCode);
            return this;
        }

        /// <summary>
        /// Clears all entry point scripts.
        /// </summary>
        /// <returns>This container for method chaining.</returns>
        public ModContainer ClearEntryPoints()
        {
            _entryPoints.Clear();
            return this;
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Mod loading must catch all exceptions to convert to ModOperationResult"
        )]
        public ModOperationResult Load()
        {
            lock (_stateLock)
            {
                if (_state == ModLoadState.Loaded)
                {
                    return ModOperationResult.Failed(
                        _state,
                        "Mod is already loaded. Call Unload() first or use Reload()."
                    );
                }

                if (
                    _state == ModLoadState.Loading
                    || _state == ModLoadState.Unloading
                    || _state == ModLoadState.Reloading
                )
                {
                    return ModOperationResult.Failed(
                        _state,
                        ZString.Concat("Cannot load mod while in state ", _state, ".")
                    );
                }

                _state = ModLoadState.Loading;
                _lastError = null;
            }

            RaiseEvent(OnLoading, new ModEventArgs(this, ModLoadState.Loading));

            try
            {
                Script newScript = CreateScript();
                ConfigureScript(newScript);
                ExecuteEntryPoints(newScript);

                lock (_stateLock)
                {
                    _script = newScript;
                    _state = ModLoadState.Loaded;
                }

                RaiseEvent(OnLoaded, new ModEventArgs(this, ModLoadState.Loaded));
                return ModOperationResult.Succeeded(
                    ModLoadState.Loaded,
                    ZString.Concat("Mod '", DisplayName, "' loaded successfully.")
                );
            }
            catch (Exception ex)
            {
                lock (_stateLock)
                {
                    _script = null;
                    _state = ModLoadState.Faulted;
                    _lastError = ex;
                }

                RaiseErrorEvent("Load", ex);
                return ModOperationResult.Failed(
                    ModLoadState.Faulted,
                    ex,
                    ZString.Concat("Failed to load mod '", DisplayName, "'.")
                );
            }
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Mod unloading must catch all exceptions to convert to ModOperationResult"
        )]
        public ModOperationResult Unload()
        {
            Script scriptToCleanup;

            lock (_stateLock)
            {
                if (_state == ModLoadState.Unloaded)
                {
                    return ModOperationResult.Succeeded(
                        ModLoadState.Unloaded,
                        "Mod is already unloaded."
                    );
                }

                if (
                    _state == ModLoadState.Loading
                    || _state == ModLoadState.Unloading
                    || _state == ModLoadState.Reloading
                )
                {
                    return ModOperationResult.Failed(
                        _state,
                        ZString.Concat("Cannot unload mod while in state ", _state, ".")
                    );
                }

                scriptToCleanup = _script;
                _state = ModLoadState.Unloading;
            }

            RaiseEvent(OnUnloading, new ModEventArgs(this, ModLoadState.Unloading));

            try
            {
                // Give the mod a chance to clean up
                if (scriptToCleanup != null)
                {
                    InvokeUnloadHandler(scriptToCleanup);
                }

                lock (_stateLock)
                {
                    _script = null;
                    _state = ModLoadState.Unloaded;
                    _lastError = null;
                }

                RaiseEvent(OnUnloaded, new ModEventArgs(this, ModLoadState.Unloaded));
                return ModOperationResult.Succeeded(
                    ModLoadState.Unloaded,
                    ZString.Concat("Mod '", DisplayName, "' unloaded successfully.")
                );
            }
            catch (Exception ex)
            {
                lock (_stateLock)
                {
                    // Even on error, we mark as unloaded since the script is gone
                    _script = null;
                    _state = ModLoadState.Unloaded;
                    _lastError = ex;
                }

                RaiseErrorEvent("Unload", ex);
                return ModOperationResult.Failed(
                    ModLoadState.Unloaded,
                    ex,
                    ZString.Concat(
                        "Error during mod '",
                        DisplayName,
                        "' unload (mod is still unloaded)."
                    )
                );
            }
        }

        /// <inheritdoc/>
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Mod reloading must catch all exceptions to convert to ModOperationResult"
        )]
        public ModOperationResult Reload()
        {
            Script scriptToCleanup;

            lock (_stateLock)
            {
                if (
                    _state == ModLoadState.Loading
                    || _state == ModLoadState.Unloading
                    || _state == ModLoadState.Reloading
                )
                {
                    return ModOperationResult.Failed(
                        _state,
                        ZString.Concat("Cannot reload mod while in state ", _state, ".")
                    );
                }

                scriptToCleanup = _script;
                _state = ModLoadState.Reloading;
                _lastError = null;
            }

            RaiseEvent(OnReloading, new ModEventArgs(this, ModLoadState.Reloading));

            try
            {
                // Clean up old script
                if (scriptToCleanup != null)
                {
                    InvokeUnloadHandler(scriptToCleanup);
                }

                // Create and configure new script
                Script newScript = CreateScript();
                ConfigureScript(newScript);
                ExecuteEntryPoints(newScript);

                lock (_stateLock)
                {
                    _script = newScript;
                    _state = ModLoadState.Loaded;
                }

                RaiseEvent(OnReloaded, new ModEventArgs(this, ModLoadState.Loaded));
                return ModOperationResult.Succeeded(
                    ModLoadState.Loaded,
                    ZString.Concat("Mod '", DisplayName, "' reloaded successfully.")
                );
            }
            catch (Exception ex)
            {
                lock (_stateLock)
                {
                    _script = null;
                    _state = ModLoadState.Faulted;
                    _lastError = ex;
                }

                RaiseErrorEvent("Reload", ex);
                return ModOperationResult.Failed(
                    ModLoadState.Faulted,
                    ex,
                    ZString.Concat("Failed to reload mod '", DisplayName, "'.")
                );
            }
        }

        /// <inheritdoc/>
        public DynValue DoString(string code, string codeFriendlyName = null)
        {
            Script script = GetLoadedScriptOrThrow();
            return script.DoString(code, null, codeFriendlyName);
        }

        /// <inheritdoc/>
        public DynValue CallFunction(string functionName, params object[] args)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException(
                    "Function name cannot be null or empty.",
                    nameof(functionName)
                );
            }

            Script script = GetLoadedScriptOrThrow();
            DynValue function = script.Globals.Get(functionName);

            if (function.Type != DataType.Function)
            {
                using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
                sb.Append("Global '");
                sb.Append(functionName);
                sb.Append("' is not a function (type: ");
                sb.Append(function.Type.ToLuaDebuggerString());
                sb.Append(").");
                throw new ScriptRuntimeException(sb.ToString());
            }

            return script.Call(function, args);
        }

        /// <inheritdoc/>
        public DynValue GetGlobal(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Global name cannot be null or empty.", nameof(name));
            }

            Script script = GetLoadedScriptOrThrow();
            return script.Globals.Get(name);
        }

        /// <inheritdoc/>
        public void SetGlobal(string name, DynValue value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Global name cannot be null or empty.", nameof(name));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Script script = GetLoadedScriptOrThrow();
            script.Globals.Set(name, value);
        }

        private Script CreateScript()
        {
            if (ScriptFactory != null)
            {
                return ScriptFactory(this);
            }

            ScriptOptions options = ScriptOptions ?? Script.DefaultOptions;
            return new Script(CoreModules, options);
        }

        private void ConfigureScript(Script script)
        {
            // Set mod metadata as globals
            script.Globals["MOD_ID"] = DynValue.NewString(ModId);
            script.Globals["MOD_NAME"] = DynValue.NewString(DisplayName);

            // Allow custom configuration
            ScriptConfigurator?.Invoke(this, script);
        }

        private void ExecuteEntryPoints(Script script)
        {
            for (int i = 0; i < _entryPoints.Count; i++)
            {
                string entryPoint = _entryPoints[i];
                string friendlyName = ZString.Concat(ModId, ":entry[", i, "]");
                script.DoString(entryPoint, null, friendlyName);
            }
        }

        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Cleanup handlers must not throw; errors are intentionally swallowed"
        )]
        private void InvokeUnloadHandler(Script script)
        {
            // First try to call a Lua-side cleanup function
            DynValue onUnload = script.Globals.Get("on_unload");
            if (onUnload.Type == DataType.Function)
            {
                try
                {
                    script.Call(onUnload);
                }
                catch
                {
                    // Ignore errors in cleanup
                }
            }

            // Then invoke the C# handler
            UnloadHandler?.Invoke(this, script);
        }

        private Script GetLoadedScriptOrThrow()
        {
            lock (_stateLock)
            {
                if (_state != ModLoadState.Loaded || _script == null)
                {
                    throw new InvalidOperationException(
                        ZString.Concat("Mod '", ModId, "' is not loaded (state: ", _state, ").")
                    );
                }

                return _script;
            }
        }

        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Event handlers must not throw; errors are intentionally swallowed"
        )]
        private void RaiseEvent(EventHandler<ModEventArgs> handler, ModEventArgs args)
        {
            try
            {
                handler?.Invoke(this, args);
            }
            catch
            {
                // Ignore errors in event handlers
            }
        }

        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "Event handlers must not throw; errors are intentionally swallowed"
        )]
        private void RaiseErrorEvent(string operation, Exception error)
        {
            ModLoadState currentState;
            lock (_stateLock)
            {
                currentState = _state;
            }

            try
            {
                OnError?.Invoke(this, new ModErrorEventArgs(this, currentState, error, operation));
            }
            catch
            {
                // Ignore errors in event handlers
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ZString.Concat("ModContainer[", ModId, "] (", State, ")");
        }
    }
}
