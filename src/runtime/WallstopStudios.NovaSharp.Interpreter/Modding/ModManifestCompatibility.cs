namespace WallstopStudios.NovaSharp.Interpreter.Modding
{
    using System;
    using System.IO;
    using System.Text.Json;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Utility helpers that detect and apply <c>mod.json</c>-declared compatibility settings.
    /// Hosts (CLI, Unity, debugger bridges, etc.) can call these helpers before instantiating scripts so
    /// the correct Lua profile is baked into the ScriptOptions.
    /// </summary>
    public static class ModManifestCompatibility
    {
        /// <summary>
        /// Attempts to apply compatibility metadata by locating a <c>mod.json</c> beside the specified script.
        /// </summary>
        /// <param name="scriptPath">Script file path that may live under a mod directory.</param>
        /// <param name="options">Script options to configure when a manifest is present.</param>
        /// <param name="hostCompatibility">Optional host profile used to emit "newer than host" warnings.</param>
        /// <param name="infoSink">Optional callback that receives informational messages (e.g., active profile).</param>
        /// <param name="warningSink">Optional callback that receives warning messages (e.g., parse failures).</param>
        /// <param name="fileSystem">Optional filesystem abstraction (defaults to the physical implementation).</param>
        /// <returns><c>true</c> when a manifest was found and applied.</returns>
        public static bool TryApplyFromScriptPath(
            string scriptPath,
            ScriptOptions options,
            LuaCompatibilityVersion? hostCompatibility = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null,
            IModFileSystem fileSystem = null
        )
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return false;
            }

            IModFileSystem fs = fileSystem ?? PhysicalModFileSystem.Instance;
            string directory = ResolveDirectory(fs, scriptPath);
            return TryApplyFromDirectory(
                directory,
                options,
                hostCompatibility,
                infoSink,
                warningSink,
                fs
            );
        }

        /// <summary>
        /// Attempts to apply compatibility metadata by reading <c>mod.json</c> under the supplied directory.
        /// </summary>
        /// <param name="directory">Directory that potentially contains a manifest.</param>
        /// <param name="options">Script options to configure when a manifest is present.</param>
        /// <param name="hostCompatibility">Optional host profile used to emit "newer than host" warnings.</param>
        /// <param name="infoSink">Optional callback for informational messages.</param>
        /// <param name="warningSink">Optional callback for warnings/errors.</param>
        /// <param name="fileSystem">Optional filesystem abstraction (defaults to the physical implementation).</param>
        /// <returns><c>true</c> when a manifest was found and applied.</returns>
        public static bool TryApplyFromDirectory(
            string directory,
            ScriptOptions options,
            LuaCompatibilityVersion? hostCompatibility = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null,
            IModFileSystem fileSystem = null
        )
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(directory))
            {
                return false;
            }

            IModFileSystem fs = fileSystem ?? PhysicalModFileSystem.Instance;
            string manifestPath = Path.Combine(directory, "mod.json");

            if (!fs.FileExists(manifestPath))
            {
                return false;
            }

            try
            {
                using Stream stream = fs.OpenRead(manifestPath);
                ModManifest manifest = ModManifest.Load(stream);

                LuaCompatibilityVersion hostVersion =
                    hostCompatibility ?? Script.GlobalOptions.CompatibilityVersion;

                manifest.ApplyCompatibility(options, hostVersion, warningSink);

                if (manifest.LuaCompatibility.HasValue)
                {
                    LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                        options.CompatibilityVersion
                    );
                    infoSink?.Invoke($"Applied {profile.DisplayName} profile from {manifestPath}.");
                }

                return true;
            }
            catch (Exception ex)
                when (ex is IOException
                    || ex is UnauthorizedAccessException
                    || ex is InvalidDataException
                    || ex is JsonException
                    || ex is ArgumentException
                )
            {
                warningSink?.Invoke($"Failed to load {manifestPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resolves the directory that should be checked for <c>mod.json</c> based on a script path (or directory).
        /// </summary>
        private static string ResolveDirectory(IModFileSystem fileSystem, string scriptPath)
        {
            string fullPath;

            try
            {
                fullPath = fileSystem.GetFullPath(scriptPath);
            }
            catch (Exception ex)
                when (ex is ArgumentException
                    || ex is PathTooLongException
                    || ex is NotSupportedException
                    || ex is IOException
                    || ex is UnauthorizedAccessException
                    || ex is System.Security.SecurityException
                )
            {
                fullPath = scriptPath;
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            return fileSystem.DirectoryExists(fullPath)
                ? fullPath
                : fileSystem.GetDirectoryName(fullPath);
        }

        /// <summary>
        /// Creates a <see cref="Script"/> configured with a manifest-driven compatibility profile.
        /// </summary>
        public static Script CreateScriptFromDirectory(
            string directory,
            CoreModules modules = CoreModules.PresetComplete,
            ScriptOptions baseOptions = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null,
            IModFileSystem fileSystem = null
        )
        {
            ScriptOptions options = new(baseOptions ?? Script.DefaultOptions);
            IModFileSystem fs = fileSystem ?? PhysicalModFileSystem.Instance;

            TryApplyFromDirectory(
                directory,
                options,
                Script.GlobalOptions.CompatibilityVersion,
                infoSink,
                warningSink,
                fs
            );

            return new Script(modules, options);
        }
    }
}
