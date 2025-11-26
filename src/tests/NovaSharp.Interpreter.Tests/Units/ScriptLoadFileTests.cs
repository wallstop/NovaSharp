namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptLoadFileTests
    {
        [Test]
        public void LoadFileThrowsWhenScriptLoaderReturnsNull()
        {
            ScriptOptions options = new ScriptOptions { ScriptLoader = new StubScriptLoader(null) };
            Script script = new Script(CoreModules.Basic, options);

            InvalidCastException ex = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            Assert.That(ex.Message, Does.Contain("Unexpected null from IScriptLoader.LoadFile"));
        }

        [Test]
        public void LoadFileThrowsWhenScriptLoaderReturnsUnsupportedType()
        {
            object unexpected = new object();
            ScriptOptions options = new ScriptOptions
            {
                ScriptLoader = new StubScriptLoader(unexpected),
            };
            Script script = new Script(CoreModules.Basic, options);

            InvalidCastException ex = Assert.Throws<InvalidCastException>(() =>
                script.LoadFile("test.lua")
            );

            Assert.Multiple(() =>
            {
                Assert.That(ex.Message, Does.Contain("Unsupported return type"));
                Assert.That(ex.Message, Does.Contain(unexpected.GetType().Name));
            });
        }

        private sealed class StubScriptLoader : IScriptLoader
        {
            private readonly object _returnValue;

            public StubScriptLoader(object returnValue)
            {
                _returnValue = returnValue;
            }

            public object LoadFile(string file, Table globalContext)
            {
                return _returnValue;
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
