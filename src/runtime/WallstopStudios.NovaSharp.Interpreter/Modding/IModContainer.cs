namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Defines the contract for a mod container that provides isolated script execution
    /// with lifecycle management (load, unload, reload).
    /// </summary>
    public interface IModContainer
    {
        /// <summary>
        /// Gets the unique identifier for this mod.
        /// </summary>
        public string ModId { get; }

        /// <summary>
        /// Gets the display name of this mod.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the current load state of this mod.
        /// </summary>
        public ModLoadState State { get; }

        /// <summary>
        /// Gets the underlying <see cref="Script"/> instance for this mod.
        /// Returns <c>null</c> if the mod is not loaded.
        /// </summary>
        public Script Script { get; }

        /// <summary>
        /// Gets the mod's global table (environment).
        /// Returns <c>null</c> if the mod is not loaded.
        /// </summary>
        public Table Globals { get; }

        /// <summary>
        /// Gets the last error that occurred during a mod operation, if any.
        /// </summary>
        public Exception LastError { get; }

        /// <summary>
        /// Gets or sets the options used when creating the mod's <see cref="Script"/> instance.
        /// Changes only take effect on the next load or reload.
        /// </summary>
        public ScriptOptions ScriptOptions { get; set; }

        /// <summary>
        /// Gets or sets the core modules to register when loading the mod.
        /// Changes only take effect on the next load or reload.
        /// </summary>
        public CoreModules CoreModules { get; set; }

        /// <summary>
        /// Occurs when the mod begins loading.
        /// </summary>
        public event EventHandler<ModEventArgs> OnLoading;

        /// <summary>
        /// Occurs when the mod has finished loading successfully.
        /// </summary>
        public event EventHandler<ModEventArgs> OnLoaded;

        /// <summary>
        /// Occurs when the mod begins unloading.
        /// </summary>
        public event EventHandler<ModEventArgs> OnUnloading;

        /// <summary>
        /// Occurs when the mod has finished unloading.
        /// </summary>
        public event EventHandler<ModEventArgs> OnUnloaded;

        /// <summary>
        /// Occurs when the mod begins reloading.
        /// </summary>
        public event EventHandler<ModEventArgs> OnReloading;

        /// <summary>
        /// Occurs when the mod has finished reloading.
        /// </summary>
        public event EventHandler<ModEventArgs> OnReloaded;

        /// <summary>
        /// Occurs when a mod operation fails.
        /// </summary>
        public event EventHandler<ModErrorEventArgs> OnError;

        /// <summary>
        /// Loads the mod by creating a new <see cref="Script"/> instance and executing
        /// the configured entry point script(s).
        /// </summary>
        /// <returns>The result of the load operation.</returns>
        public ModOperationResult Load();

        /// <summary>
        /// Unloads the mod by disposing the <see cref="Script"/> instance and clearing state.
        /// </summary>
        /// <returns>The result of the unload operation.</returns>
        public ModOperationResult Unload();

        /// <summary>
        /// Reloads the mod by unloading and then loading it again.
        /// </summary>
        /// <returns>The result of the reload operation.</returns>
        public ModOperationResult Reload();

        /// <summary>
        /// Executes a Lua string in the context of this mod.
        /// </summary>
        /// <param name="code">The Lua code to execute.</param>
        /// <param name="codeFriendlyName">Optional friendly name for error messages.</param>
        /// <returns>The result of the execution.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the mod is not loaded.</exception>
        public DynValue DoString(string code, string codeFriendlyName = null);

        /// <summary>
        /// Invokes a global function defined in this mod.
        /// </summary>
        /// <param name="functionName">The name of the function to call.</param>
        /// <param name="args">Arguments to pass to the function.</param>
        /// <returns>The result of the function call.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the mod is not loaded.</exception>
        public DynValue CallFunction(string functionName, params object[] args);

        /// <summary>
        /// Gets a global value from the mod's environment.
        /// </summary>
        /// <param name="name">The name of the global.</param>
        /// <returns>The value, or <see cref="DynValue.Nil"/> if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the mod is not loaded.</exception>
        public DynValue GetGlobal(string name);

        /// <summary>
        /// Sets a global value in the mod's environment.
        /// </summary>
        /// <param name="name">The name of the global.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">Thrown if the mod is not loaded.</exception>
        public void SetGlobal(string name, DynValue value);
    }
}
