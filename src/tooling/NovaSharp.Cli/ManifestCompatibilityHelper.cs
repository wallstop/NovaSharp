namespace NovaSharp.Cli
{
    using System;
    using System.IO;
    using System.Text.Json;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.Modding;

    internal static class ManifestCompatibilityHelper
    {
        public static string ResolveScriptPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return path;
            }
        }

        public static bool TryApplyManifestCompatibility(string scriptPath, ScriptOptions options)
        {
            if (string.IsNullOrEmpty(scriptPath))
            {
                return false;
            }

            string directory = Directory.Exists(scriptPath)
                ? scriptPath
                : Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory();
            string manifestPath = Path.Combine(directory, "mod.json");

            if (!File.Exists(manifestPath))
            {
                return false;
            }

#pragma warning disable CA1031 // CLI emits friendly warnings instead of crashing on manifest load failures.
            try
            {
                using FileStream stream = File.OpenRead(manifestPath);
                ModManifest manifest = ModManifest.Load(stream);

                manifest.ApplyCompatibility(
                    options,
                    Script.GlobalOptions.CompatibilityVersion,
                    warning =>
                    {
                        Console.WriteLine($"[compatibility] {warning}");
                    }
                );

                if (manifest.LuaCompatibility.HasValue)
                {
                    LuaCompatibilityProfile profile = LuaCompatibilityProfile.ForVersion(
                        options.CompatibilityVersion
                    );
                    Console.WriteLine(
                        $"[compatibility] Applied {profile.DisplayName} profile from {manifestPath}."
                    );
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
                Console.WriteLine($"[compatibility] Failed to load {manifestPath}: {ex.Message}");
                return false;
            }
#pragma warning restore CA1031
        }
    }
}
