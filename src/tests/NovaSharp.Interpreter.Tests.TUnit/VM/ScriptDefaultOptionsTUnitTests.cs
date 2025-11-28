namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;

    public sealed class ScriptDefaultOptionsTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task DefaultOptionsScriptLoaderPersistsAcrossNewScripts()
        {
            TrackingScriptLoader loader = new();
            IScriptLoader prior = Script.DefaultOptions.ScriptLoader;
            try
            {
                Script.DefaultOptions.ScriptLoader = loader;

                Script script = new();

                await Assert.That(ReferenceEquals(script.Options.ScriptLoader, loader)).IsTrue();
            }
            finally
            {
                Script.DefaultOptions.ScriptLoader = prior;
            }
        }

        private sealed class TrackingScriptLoader : IScriptLoader
        {
            public object LoadFile(string file, Table globalContext)
            {
                return "return";
            }

            public string ResolveFileName(string filename, Table globalContext)
            {
                return filename;
            }

            public string ResolveModuleName(string modname, Table globalContext)
            {
                return modname;
            }
        }
    }
}
