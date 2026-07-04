namespace NovaSharp
{
    using System;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Options used to create a <see cref="LuaEngine"/>.
    /// </summary>
    public sealed class LuaEngineOptions
    {
        /// <summary>
        /// Initializes a default options instance.
        /// </summary>
        public LuaEngineOptions()
        {
            Version = LuaVersion.Latest;
            Modules = (LuaCoreModules)CoreModulePresets.Default;
            Sandbox = LuaSandboxOptions.Unrestricted;
            EnableScriptCaching = true;
            ScriptCacheMaxEntries = 64;
        }

        /// <summary>
        /// Initializes an options instance by copying an existing instance.
        /// </summary>
        public LuaEngineOptions(LuaEngineOptions defaults)
        {
            if (defaults == null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            Version = defaults.Version;
            Modules = defaults.Modules;
            Sandbox = defaults.Sandbox == null ? null : new LuaSandboxOptions(defaults.Sandbox);
            Loader = defaults.Loader;
            Time = defaults.Time;
            Random = defaults.Random;
            Print = defaults.Print;
            EnableScriptCaching = defaults.EnableScriptCaching;
            ScriptCacheMaxEntries = defaults.ScriptCacheMaxEntries;
        }

        /// <summary>
        /// Gets a reusable default options instance.
        /// </summary>
        public static LuaEngineOptions Default => new LuaEngineOptions();

        /// <summary>
        /// Gets a restrictive sandbox preset suitable as a starting point for untrusted scripts.
        /// </summary>
        public static LuaEngineOptions HardSandbox =>
            new LuaEngineOptions
            {
                Modules = (LuaCoreModules)CoreModulePresets.HardSandbox,
                Sandbox = LuaSandboxOptions.CreateRestrictive(),
            };

        /// <summary>
        /// Gets or sets the Lua compatibility version.
        /// </summary>
        public LuaVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the core modules registered in the global table.
        /// </summary>
        public LuaCoreModules Modules { get; set; }

        /// <summary>
        /// Gets or sets the sandbox options.
        /// </summary>
        public LuaSandboxOptions Sandbox { get; set; }

        /// <summary>
        /// Gets or sets the script loader.
        /// </summary>
        public ILuaScriptLoader Loader { get; set; }

        /// <summary>
        /// Gets or sets the time provider.
        /// </summary>
        public ILuaTimeProvider Time { get; set; }

        /// <summary>
        /// Gets or sets the random number provider.
        /// </summary>
        public ILuaRandomProvider Random { get; set; }

        /// <summary>
        /// Gets or sets the print handler used by Lua print/debug output.
        /// </summary>
        public Action<string> Print { get; set; }

        /// <summary>
        /// Gets or sets whether per-engine script compilation caching is enabled.
        /// </summary>
        public bool EnableScriptCaching { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of cached script entries per engine.
        /// </summary>
        public int ScriptCacheMaxEntries { get; set; }
    }
}
