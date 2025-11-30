namespace NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;

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
        private readonly string _file;
        private readonly LuaCompatibilityVersion _compatibilityVersion;

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
            ScriptOptions options = new(Script.DefaultOptions)
            {
                DebugPrint = Print,
                UseLuaErrorLocations = true,
                CompatibilityVersion = _compatibilityVersion,
            };

            Script script = new(options);
            if (script.Options.ScriptLoader is not ScriptLoaderBase)
            {
                script.Options.ScriptLoader = new FileSystemScriptLoader();
            }
            ConfigureScriptLoader(script);
            script.Globals.Set("arg", DynValue.NewTable(script));
            string friendlyName = _file.Replace('\\', '/');
            script.DoFile(GetAbsoluteTestPath(_file), null, friendlyName);
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
    }
}
