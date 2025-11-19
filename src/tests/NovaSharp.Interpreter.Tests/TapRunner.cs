namespace NovaSharp.Interpreter.Tests
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

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

    public class TapRunner
    {
        private readonly string _file;

        /// <summary>
        /// Prints the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        public void Print(string str)
        {
            Assert.That(str.Trim().StartsWith("not ok"), Is.False, $"TAP fail ({_file}) : {str}");
        }

        public TapRunner(string filename)
        {
            _file = filename;
        }

        public void Run()
        {
            Script s = new() { Options = { DebugPrint = Print, UseLuaErrorLocations = true } };

            ConfigureScriptLoader(s);
            s.Globals.Set("arg", DynValue.NewTable(s));
            string friendlyName = _file.Replace('\\', '/');
            s.DoFile(GetAbsoluteTestPath(_file), null, friendlyName);
        }

        public static void Run(string filename)
        {
            TapRunner t = new(filename);
            t.Run();
        }

        private static void ConfigureScriptLoader(Script script)
        {
            if (script.Options.ScriptLoader is not ScriptLoaderBase loader)
            {
                throw new InvalidOperationException(
                    "TapRunner requires a ScriptLoaderBase loader."
                );
            }

            string testDirectory =
                TestContext.CurrentContext?.TestDirectory ?? AppContext.BaseDirectory;

            // Normalize to forward slashes because the loader simply does string replacement.
            string modulesDirectory = Path.Combine(testDirectory, "TestMore", "Modules")
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Replace('\\', '/');

            loader.ModulePaths = new[] { $"{modulesDirectory}/?", $"{modulesDirectory}/?.lua" };
        }

        private static string GetAbsoluteTestPath(string relativePath)
        {
            string testDirectory =
                TestContext.CurrentContext?.TestDirectory ?? AppContext.BaseDirectory;
            string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(testDirectory, normalized);
        }
    }
}
