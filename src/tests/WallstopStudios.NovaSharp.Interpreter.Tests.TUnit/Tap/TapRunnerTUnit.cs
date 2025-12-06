namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Modules;

#if !EMBEDTEST
    internal sealed class TestsScriptLoader : ScriptLoaderBase
    {
        public override bool ScriptFileExists(string name)
        {
            return File.Exists(name);
        }

        public override object LoadFile(string file, Table globalContext)
        {
            return new FileStream(file, FileMode.Open, FileAccess.Read);
        }
    }
#endif

    internal sealed class TapRunnerTUnit
    {
        private static readonly HashSet<string> SkippedSuites = new(
            StringComparer.OrdinalIgnoreCase
        )
        { };

        private readonly string _file;
        private readonly LuaCompatibilityVersion _compatibilityVersion;
        private bool _skipAllRequested;

        public TapRunnerTUnit(string filename, LuaCompatibilityVersion? compatibilityVersion = null)
        {
            ArgumentNullException.ThrowIfNull(filename);
            _file = filename;
            _compatibilityVersion =
                compatibilityVersion ?? Script.GlobalOptions.CompatibilityVersion;
        }

        public static void Run(
            string filename,
            LuaCompatibilityVersion? compatibilityVersion = null
        )
        {
            TapRunnerTUnit runner = new(filename, compatibilityVersion);
            runner.Run();
        }

        public void Run()
        {
            string normalizedRelativePath = _file.Replace('\\', '/');

            if (SkippedSuites.Contains(normalizedRelativePath))
            {
                Print($"1..0 # SKIP {_file} requires the Lua debug library");
                _skipAllRequested = true;
                return;
            }

            ScriptOptions options = new(Script.DefaultOptions)
            {
                DebugPrint = Print,
                UseLuaErrorLocations = true,
                CompatibilityVersion = _compatibilityVersion,
                ForceUtcDateTime = true,
            };

            Script script = new(CoreModules.PresetComplete, options);
            if (script.Options.ScriptLoader is not ScriptLoaderBase)
            {
                script.Options.ScriptLoader = new FileSystemScriptLoader();
            }
            ConfigureScriptLoader(script);
            string suiteDirectory =
                Path.GetDirectoryName(GetAbsoluteTestPath(_file)) ?? GetTestDirectory();
            SeedTapGlobals(script, _compatibilityVersion, suiteDirectory);
            string friendlyName = _file.Replace('\\', '/');
            try
            {
                script.DoFile(GetAbsoluteTestPath(_file), null, friendlyName);
            }
            catch (ScriptRuntimeException ex)
            {
                if (
                    _skipAllRequested
                    && ex.DecoratedMessage != null
                    && ex.DecoratedMessage.Contains(
                        "plan was already output",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return;
                }

                throw;
            }
        }

        private void Print(string value)
        {
            if (value == null)
            {
                return;
            }

            string trimmed = value.Trim();
            if (trimmed.StartsWith("not ok", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"TAP fail ({_file}) : {value}");
            }

            if (trimmed.StartsWith("1..0 # SKIP", StringComparison.OrdinalIgnoreCase))
            {
                _skipAllRequested = true;
            }
        }

        private static void ConfigureScriptLoader(Script script)
        {
            if (script.Options.ScriptLoader is not ScriptLoaderBase loader)
            {
                throw new InvalidOperationException(
                    "TapRunner requires a ScriptLoaderBase loader."
                );
            }

#if !EMBEDTEST
            if (loader is not TestsScriptLoader)
            {
                script.Options.ScriptLoader = new TestsScriptLoader();
                loader = (ScriptLoaderBase)script.Options.ScriptLoader;
            }
#endif

            string testDirectory = GetTestDirectory();
            string modulesDirectory = Path.Combine(testDirectory, "TestMore", "Modules")
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');

            loader.ModulePaths = new[] { $"{modulesDirectory}/?", $"{modulesDirectory}/?.lua" };
        }

        private static string GetAbsoluteTestPath(string relativePath)
        {
            string testDirectory = GetTestDirectory();
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(testDirectory, normalized);
        }

        private static string GetTestDirectory()
        {
            return AppContext.BaseDirectory;
        }

        private static void SeedTapGlobals(
            Script script,
            LuaCompatibilityVersion compatibilityVersion,
            string testDirectory
        )
        {
            ArgumentNullException.ThrowIfNull(script);

            string luaExecutable = GetLuaExecutableHint();

            Table argTable = new(script);
            argTable.Set(DynValue.NewNumber(-1), DynValue.NewString(luaExecutable));
            script.Globals.Set("arg", DynValue.NewTable(argTable));

            Table platform = new(script);
            platform.Set("lua", DynValue.NewString(luaExecutable));
            platform.Set("compat", DynValue.NewBoolean(false));
            platform.Set("intsize", DynValue.NewNumber(IntPtr.Size));
            platform.Set("osname", DynValue.NewString(GetPlatformName()));
            TapStdinHelper.Register(script, platform, compatibilityVersion, testDirectory);
            script.Globals.Set("platform", DynValue.NewTable(platform));
        }

        private static string GetLuaExecutableHint()
        {
            string cliExecutablePath = ResolveCliExecutablePath();
            if (!string.IsNullOrEmpty(cliExecutablePath) && File.Exists(cliExecutablePath))
            {
                return $"dotnet \"{cliExecutablePath}\"";
            }

            return "lua";
        }

        private static string ResolveCliExecutablePath()
        {
            string baseDirectory = AppContext.BaseDirectory;

            string candidate = Path.Combine(
                baseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "tooling",
                "WallstopStudios.NovaSharp.Cli",
                "bin",
                "Release",
                "net8.0",
                "WallstopStudios.NovaSharp.Cli.dll"
            );

            try
            {
                return Path.GetFullPath(candidate);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (PathTooLongException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string GetPlatformName()
        {
            if (OperatingSystem.IsWindows())
            {
                return "MSWin32";
            }

            if (OperatingSystem.IsMacOS())
            {
                return "Darwin";
            }

            if (OperatingSystem.IsLinux())
            {
                return "Linux";
            }

            return "Unknown";
        }
    }
}
