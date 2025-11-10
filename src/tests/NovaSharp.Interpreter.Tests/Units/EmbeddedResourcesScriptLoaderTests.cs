namespace NovaSharp.Interpreter.Tests.Units
{
    using System.IO;
    using System.Reflection;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class EmbeddedResourcesScriptLoaderTests
    {
        [Test]
        public void ScriptFileExistsHandlesEmbeddedResource()
        {
            EmbeddedResourcesScriptLoader loader = new EmbeddedResourcesScriptLoader(
                resourceAssembly: Assembly.GetExecutingAssembly()
            );

            Assert.Multiple(() =>
            {
                Assert.That(loader.ScriptFileExists("Resources/embedded.lua"), Is.True);
                Assert.That(loader.ScriptFileExists("Resources/missing.lua"), Is.False);
            });
        }

        [Test]
        public void LoadFileReturnsManifestStream()
        {
            EmbeddedResourcesScriptLoader loader = new EmbeddedResourcesScriptLoader(
                resourceAssembly: Assembly.GetExecutingAssembly()
            );

            using Stream stream = (Stream)
                loader.LoadFile("Resources/embedded.lua", globalContext: null);

            using StreamReader reader = new StreamReader(stream);
            string contents = reader.ReadToEnd();

            Assert.That(contents, Does.Contain("hello from embedded resource"));
        }
    }
}
