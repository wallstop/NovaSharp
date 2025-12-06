namespace WallstopStudios.NovaSharp.Interpreter.REPL
{
#if !(PCL || ENABLE_DOTNET || NETFX_CORE)
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using System;
    using System.Collections.Generic;
    using Loaders;

    /// <summary>
    /// A script loader loading scripts directly from the file system (does not go through platform object)
    /// AND starts with module paths taken from environment variables (again, not going through the platform object).
    ///
    /// The paths are preconstructed using :
    ///		* The NOVASHARP_PATH environment variable if it exists
    ///		* The LUA_PATH_5_2 environment variable if NOVASHARP_PATH does not exist
    ///		* The LUA_PATH environment variable if LUA_PATH_5_2 does not exist
    ///		* The "?;?.lua" path if all the above fail
    ///
    /// Also, every time a module is require(d), the "LUA_PATH" global variable is checked. If it exists, those paths
    /// will be used to load the module instead of the global ones.
    /// </summary>
    public class ReplInterpreterScriptLoader : FileSystemScriptLoader
    {
        private readonly Func<string, string> _environmentVariableProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplInterpreterScriptLoader"/> class.
        /// </summary>
        public ReplInterpreterScriptLoader()
            : this(Environment.GetEnvironmentVariable) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplInterpreterScriptLoader"/> class using a custom environment provider.
        /// </summary>
        /// <param name="environmentVariableProvider">Delegate used to read environment variables (defaults to <see cref="Environment.GetEnvironmentVariable(string)"/>).</param>
        protected ReplInterpreterScriptLoader(Func<string, string> environmentVariableProvider)
        {
            _environmentVariableProvider =
                environmentVariableProvider
                ?? throw new ArgumentNullException(nameof(environmentVariableProvider));

            ModulePaths = TryLoadEnvironmentPaths(NovaSharpPathEnvironmentVariable);

            if (ModulePaths == null)
            {
                ModulePaths = TryLoadEnvironmentPaths("LUA_PATH_5_2");
            }

            if (ModulePaths == null)
            {
                ModulePaths = TryLoadEnvironmentPaths("LUA_PATH");
            }

            if (ModulePaths == null)
            {
                ModulePaths = UnpackStringPaths("?;?.lua");
            }
        }

        /// <summary>
        /// Resolves the name of a module to a filename (which will later be passed to OpenScriptFile).
        /// The resolution happens first on paths included in the LUA_PATH global variable, and -
        /// if the variable does not exist - by consulting the
        /// ScriptOptions.ModulesPaths array. Override to provide a different behaviour.
        /// </summary>
        /// <param name="modname">The modname.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns></returns>
        public override string ResolveModuleName(string modname, Table globalContext)
        {
            if (globalContext == null)
            {
                throw new ArgumentNullException(nameof(globalContext));
            }

            DynValue s = globalContext.RawGet("LUA_PATH");

            if (s != null && s.Type == DataType.String)
            {
                return ResolveModuleName(modname, UnpackStringPaths(s.String));
            }
            else
            {
                return base.ResolveModuleName(modname, globalContext);
            }
        }

        private IReadOnlyList<string> TryLoadEnvironmentPaths(string variable)
        {
            string env = _environmentVariableProvider(variable);
            if (string.IsNullOrWhiteSpace(env))
            {
                return null;
            }

            IReadOnlyList<string> paths = UnpackStringPaths(env);
            return (paths.Count > 0) ? paths : null;
        }
    }
}


#endif
