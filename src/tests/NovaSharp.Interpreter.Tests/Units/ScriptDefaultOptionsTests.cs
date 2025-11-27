namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptDefaultOptionsTests
    {
        private IScriptLoader _originalLoader;

        [SetUp]
        public void SetUp()
        {
            _originalLoader = Script.DefaultOptions.ScriptLoader;
        }

        [TearDown]
        public void TearDown()
        {
            Script.DefaultOptions.ScriptLoader = _originalLoader;
        }

        [Test]
        public void DefaultOptionsScriptLoaderPersistsAcrossNewScripts()
        {
            TrackingScriptLoader loader = new();

            Script.DefaultOptions.ScriptLoader = loader;

            Script script = new();

            Assert.That(script.Options.ScriptLoader, Is.SameAs(loader));
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
