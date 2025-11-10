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
            // System.Diagnostics.Debug.WriteLine(str);

            Assert.That(str.Trim().StartsWith("not ok"), Is.False, $"TAP fail ({_file}) : {str}");
        }

        public TapRunner(string filename)
        {
            _file = filename;
        }

        public void Run()
        {
            Script s = new() { Options = { DebugPrint = Print, UseLuaErrorLocations = true } };

#if PCL
#if EMBEDTEST
            S.Options.ScriptLoader = new EmbeddedResourcesScriptLoader(
                Assembly.GetExecutingAssembly()
            );
#else
            S.Options.ScriptLoader = new TestsScriptLoader();
#endif
#endif

            s.Globals.Set("arg", DynValue.NewTable(s));

            ((ScriptLoaderBase)s.Options.ScriptLoader).ModulePaths = new string[]
            {
                "TestMore/Modules/?",
                "TestMore/Modules/?.lua",
            };

            s.DoFile(_file);
        }

        public static void Run(string filename)
        {
            TapRunner t = new(filename);
            t.Run();
        }
    }
}
