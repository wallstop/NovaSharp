namespace WallstopStudios.NovaSharp.Interpreter.Loaders
{
    using System;
    using System.Collections.Generic;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Utilities;

    /// <summary>
    /// A base implementation of IScriptLoader, offering resolution of module names.
    /// </summary>
    public abstract class ScriptLoaderBase : IScriptLoader
    {
        private static readonly char[] ModulePathSeparators = new[] { ';' };
        private static readonly IReadOnlyList<string> EmptyModulePaths = Array.Empty<string>();
        protected internal const string NovaSharpPathEnvironmentVariable = "NOVASHARP_PATH";

        /// <summary>
        /// Checks if a script file exists.
        /// </summary>
        /// <param name="name">The script filename.</param>
        /// <returns></returns>
        public abstract bool ScriptFileExists(string name);

        /// <summary>
        /// Opens a file for reading the script code.
        /// It can return either a string, a byte[] or a Stream.
        /// If a byte[] is returned, the content is assumed to be a serialized (dumped) bytecode. If it's a string, it's
        /// assumed to be either a script or the output of a string.dump call. If a Stream, autodetection takes place.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>
        /// A string, a byte[] or a Stream.
        /// </returns>
        public abstract object LoadFile(string file, Table globalContext);

        /// <summary>
        /// Resolves the name of a module on a set of paths.
        /// </summary>
        /// <param name="modname">The modname.</param>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        protected virtual string ResolveModuleName(string modname, IEnumerable<string> paths)
        {
            if (modname == null)
            {
                throw new ArgumentNullException(nameof(modname));
            }

            if (paths == null)
            {
                return null;
            }

            string normalizedModule = NormalizeModuleName(modname);

            foreach (string path in paths)
            {
                string file = path.Replace("?", normalizedModule, StringComparison.Ordinal);

                if (ScriptFileExists(file))
                {
                    return file;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the name of a module on a set of paths, returning both the resolved path
        /// and the list of paths that were searched. This is used to generate detailed error
        /// messages matching reference Lua behavior.
        /// </summary>
        /// <param name="modname">The module name to resolve.</param>
        /// <param name="paths">The paths to search.</param>
        /// <returns>A <see cref="ModuleResolutionResult"/> containing the result and searched paths.</returns>
        protected virtual ModuleResolutionResult ResolveModuleNameWithSearchedPaths(
            string modname,
            IEnumerable<string> paths
        )
        {
            if (modname == null)
            {
                throw new ArgumentNullException(nameof(modname));
            }

            if (paths == null)
            {
                return ModuleResolutionResult.NotFound(EmptyModulePaths);
            }

            string normalizedModule = NormalizeModuleName(modname);
            List<string> searchedPaths = new();

            foreach (string path in paths)
            {
                string file = path.Replace("?", normalizedModule, StringComparison.Ordinal);
                searchedPaths.Add(file);

                if (ScriptFileExists(file))
                {
                    return ModuleResolutionResult.Success(file, searchedPaths);
                }
            }

            return ModuleResolutionResult.NotFound(searchedPaths);
        }

        /// <summary>
        /// Resolves the name of a module to a filename, returning both the resolved path
        /// and the list of paths that were searched. This is used to generate detailed error
        /// messages matching reference Lua behavior when a module is not found.
        /// </summary>
        /// <param name="modname">The module name to resolve.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns>A <see cref="ModuleResolutionResult"/> containing the result and searched paths.</returns>
        public virtual ModuleResolutionResult TryResolveModuleName(
            string modname,
            Table globalContext
        )
        {
            if (modname == null)
            {
                throw new ArgumentNullException(nameof(modname));
            }

            if (globalContext == null)
            {
                throw new ArgumentNullException(nameof(globalContext));
            }

            if (!IgnoreLuaPathGlobal)
            {
                DynValue s = globalContext.RawGet("LUA_PATH");

                if (s != null && s.Type == DataType.String)
                {
                    return ResolveModuleNameWithSearchedPaths(modname, UnpackStringPaths(s.String));
                }
            }

            return ResolveModuleNameWithSearchedPaths(modname, ModulePaths);
        }

        /// <summary>
        /// Resolves the name of a module to a filename (which will later be passed to OpenScriptFile).
        /// The resolution happens first on paths included in the LUA_PATH global variable (if and only if
        /// the IgnoreLuaPathGlobal is false), and - if the variable does not exist - by consulting the
        /// ScriptOptions.ModulesPaths array. Override to provide a different behaviour.
        /// </summary>
        /// <param name="modname">The modname.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns></returns>
        public virtual string ResolveModuleName(string modname, Table globalContext)
        {
            if (modname == null)
            {
                throw new ArgumentNullException(nameof(modname));
            }

            if (globalContext == null)
            {
                throw new ArgumentNullException(nameof(globalContext));
            }

            if (!IgnoreLuaPathGlobal)
            {
                DynValue s = globalContext.RawGet("LUA_PATH");

                if (s != null && s.Type == DataType.String)
                {
                    return ResolveModuleName(modname, UnpackStringPaths(s.String));
                }
            }

            return ResolveModuleName(modname, ModulePaths);
        }

        /// <summary>
        /// Gets or sets the modules paths used by the "require" function. If null, the default paths are used (using
        /// environment variables etc.).
        /// </summary>
        public IReadOnlyList<string> ModulePaths { get; set; }

        /// <summary>
        /// Unpacks a string path in a form like "?;?.lua" to an array
        /// </summary>
        public static IReadOnlyList<string> UnpackStringPaths(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            ReadOnlySpan<char> span = str.AsSpan();
            List<string> segments = null;
            int position = 0;

            while (position <= span.Length)
            {
                ReadOnlySpan<char> remainder = span.Slice(position);
                int separatorIndex = remainder.IndexOfAny(ModulePathSeparators);
                int segmentLength = separatorIndex < 0 ? remainder.Length : separatorIndex;
                ReadOnlySpan<char> segment = remainder.Slice(0, segmentLength).TrimWhitespace();

                if (!segment.IsEmpty)
                {
                    segments ??= new List<string>();
                    segments.Add(new string(segment));
                }

                if (separatorIndex < 0)
                {
                    break;
                }

                position += segmentLength + 1;
            }

            return segments?.ToArray() ?? EmptyModulePaths;
        }

        /// <summary>
        /// Gets the default environment paths.
        /// </summary>
        public static IReadOnlyList<string> GetDefaultEnvironmentPaths()
        {
            IReadOnlyList<string> paths =
                TryUnpackEnvironmentVariable(
                    Script.GlobalOptions.Platform.GetEnvironmentVariable(
                        NovaSharpPathEnvironmentVariable
                    )
                )
                ?? TryUnpackEnvironmentVariable(
                    Script.GlobalOptions.Platform.GetEnvironmentVariable("LUA_PATH")
                );

            return paths ?? UnpackStringPaths("?;?.lua");
        }

        /// <summary>
        /// Resolves a filename [applying paths, etc.]
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="globalContext">The global context.</param>
        /// <returns></returns>
        public virtual string ResolveFileName(string filename, Table globalContext)
        {
            if (filename == null)
            {
                throw new ArgumentNullException(nameof(filename));
            }

            ReadOnlySpan<char> trimmed = filename.AsSpan().TrimWhitespace();
            return trimmed.Length == filename.Length ? filename : new string(trimmed);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the LUA_PATH global is checked or not to get the path where modules are contained.
        /// If true, the LUA_PATH global is NOT checked.
        /// </summary>
        public bool IgnoreLuaPathGlobal { get; set; }

        private static string NormalizeModuleName(string modname)
        {
            if (modname.IndexOf('.', StringComparison.Ordinal) < 0)
            {
                return modname;
            }

            return string.Create(
                modname.Length,
                modname,
                static (destination, source) =>
                {
                    for (int i = 0; i < destination.Length; i++)
                    {
                        char c = source[i];
                        destination[i] = c == '.' ? '/' : c;
                    }
                }
            );
        }

        private static IReadOnlyList<string> TryUnpackEnvironmentVariable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            IReadOnlyList<string> paths = UnpackStringPaths(value);
            return paths.Count > 0 ? paths : null;
        }
    }
}
