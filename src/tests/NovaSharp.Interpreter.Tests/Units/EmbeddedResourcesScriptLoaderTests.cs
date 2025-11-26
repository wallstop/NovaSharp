namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class EmbeddedResourcesScriptLoaderTests
    {
        private static readonly Assembly TestAssembly =
            typeof(EmbeddedResourcesScriptLoaderTests).Assembly;

        [Test]
        public void ScriptFileExistsDetectsEmbeddedResources()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("Resources/embedded.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("missing.lua"), Is.False);
            });
        }

        [Test]
        public void ScriptFileExistsNormalizesBackslashes()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            Assert.That(loader.ScriptFileExists("Resources\\embedded.lua"), Is.True);
        }

        [Test]
        public void LoadFileReturnsResourceStream()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            using Stream stream = (Stream)loader.LoadFile("Resources/embedded.lua", null);
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream.Length, Is.GreaterThan(0));
        }

#if DOTNET_CORE
        [Test]
        public void ParameterlessConstructorThrowsOnNetCore()
        {
            Assert.That(
                () => new EmbeddedResourcesScriptLoader(),
                Throws.TypeOf<NotSupportedException>()
            );
        }
#endif
    }
}
