namespace NovaSharp.Interpreter.Tests.TUnit.Loaders
{
    using System;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Loaders;

    public sealed class InvalidScriptLoaderTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ResolveFileNamePassesThroughOriginalValue()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("CustomFramework");
            string resolved = loader.ResolveFileName("path/to/script.lua", null);

            await Assert.That(resolved).IsEqualTo("path/to/script.lua");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsPlatformNotSupportedException()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("Unity");

            PlatformNotSupportedException exception = Assert.Throws<PlatformNotSupportedException>(
                () =>
                {
                    loader.LoadFile("some.lua", null);
                }
            );

            await Assert.That(exception.Message).Contains("Unity");
        }

        [global::TUnit.Core.Test]
        public async Task ResolveModuleNameThrowsPlatformNotSupportedException()
        {
            InvalidScriptLoader loader = new InvalidScriptLoader("Generic");

            PlatformNotSupportedException exception = Assert.Throws<PlatformNotSupportedException>(
                () =>
                {
                    loader.ResolveModuleName("module", null);
                }
            );

            await Assert.That(exception.Message).Contains("Generic");
        }
    }
}
