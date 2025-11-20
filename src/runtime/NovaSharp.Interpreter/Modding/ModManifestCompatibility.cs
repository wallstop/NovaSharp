namespace NovaSharp.Interpreter.Modding
{
    using System;
    using System.IO;
    using System.Text.Json;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modules;

    /// <summary>
    /// Utility helpers that detect and apply <c>mod.json</c>-declared compatibility settings.
    /// Hosts (CLI, Unity, debugger bridges, etc.) can call these helpers before instantiating scripts so
    /// the correct Lua profile is baked into the ScriptOptions.
    /// </summary>
    public static class ModManifestCompatibility
    {
        public static bool TryApplyFromScriptPath(
            string scriptPath,
            ScriptOptions options,
            LuaCompatibilityVersion? hostCompatibility = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null
        )
        {
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return false;
            }

            string directory = ResolveDirectory(scriptPath);
            return TryApplyFromDirectory(
                directory,
                options,
                hostCompatibility,
                infoSink,
                warningSink
            );
        }

        public static bool TryApplyFromDirectory(
            string directory,
            ScriptOptions options,
            LuaCompatibilityVersion? hostCompatibility = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null
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

            string manifestPath = Path.Combine(directory, "mod.json");

            if (!File.Exists(manifestPath))
            {
                return false;
            }

            try
            {
                using FileStream stream = File.OpenRead(manifestPath);
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

        private static string ResolveDirectory(string scriptPath)
        {
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(scriptPath);
            }
            catch (Exception)
            {
                fullPath = scriptPath;
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath);
        }

        /// <summary>
        /// Creates a <see cref="Script"/> configured with a manifest-driven compatibility profile.
        /// </summary>
        public static Script CreateScriptFromDirectory(
            string directory,
            CoreModules modules = CoreModules.PresetComplete,
            ScriptOptions baseOptions = null,
            Action<string> infoSink = null,
            Action<string> warningSink = null
        )
        {
            ScriptOptions options = new(baseOptions ?? Script.DefaultOptions);

            TryApplyFromDirectory(
                directory,
                options,
                Script.GlobalOptions.CompatibilityVersion,
                infoSink,
                warningSink
            );

            return new Script(modules, options);
        }
    }
}
