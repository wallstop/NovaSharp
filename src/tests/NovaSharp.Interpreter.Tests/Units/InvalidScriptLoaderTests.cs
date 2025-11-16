namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class InvalidScriptLoaderTests
    {
        [Test]
        public void ResolveFileNamePassesThroughOriginalValue()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("CustomFramework");
            string resolved = loader.ResolveFileName("path/to/script.lua", null);

            Assert.That(resolved, Is.EqualTo("path/to/script.lua"));
        }

        [Test]
        public void LoadFileThrowsPlatformNotSupportedException()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("Unity");

            PlatformNotSupportedException exception = Assert.Throws<PlatformNotSupportedException>(
                () =>
                    loader.LoadFile("some.lua", null)
            );

            Assert.That(exception.Message, Does.Contain("Unity"));
        }

        [Test]
        public void ResolveModuleNameThrowsPlatformNotSupportedException()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("Generic");

            PlatformNotSupportedException exception = Assert.Throws<PlatformNotSupportedException>(
                () =>
                    loader.ResolveModuleName("module", null)
            );

            Assert.That(exception.Message, Does.Contain("Generic"));
        }
    }
}
