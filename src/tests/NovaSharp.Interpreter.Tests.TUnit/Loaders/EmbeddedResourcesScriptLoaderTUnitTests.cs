namespace NovaSharp.Interpreter.Tests.TUnit.Loaders
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Loaders;

    public sealed class EmbeddedResourcesScriptLoaderTUnitTests
    {
        private static readonly Assembly TestAssembly =
            typeof(EmbeddedResourcesScriptLoaderTUnitTests).Assembly;

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsDetectsEmbeddedResources()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            await Assert.That(loader.ScriptFileExists("Resources/embedded.lua")).IsTrue();
            await Assert.That(loader.ScriptFileExists("missing.lua")).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsNormalizesBackslashes()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            await Assert.That(loader.ScriptFileExists("Resources\\embedded.lua")).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsThrowsWhenNameIsNull()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ScriptFileExists(null);
            });

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.ParamName).IsEqualTo("file");
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileReturnsResourceStream()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            using Stream stream = (Stream)loader.LoadFile("Resources/embedded.lua", null);

            await Assert.That(stream).IsNotNull();
            await Assert.That(stream.Length).IsGreaterThan(0);
        }

        [global::TUnit.Core.Test]
        public async Task LoadFileThrowsWhenNameIsNull()
        {
            EmbeddedResourcesScriptLoader loader = new(TestAssembly);

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.LoadFile(null, null);
            });

            await Assert.That(exception).IsNotNull();
            await Assert.That(exception.ParamName).IsEqualTo("file");
        }

#if DOTNET_CORE
        [global::TUnit.Core.Test]
        public async Task ParameterlessConstructorThrowsOnNetCore()
        {
            NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            {
                _ = new EmbeddedResourcesScriptLoader();
            });

            await Assert.That(exception).IsNotNull();
        }
#else
        [global::TUnit.Core.Test]
        public async Task ParameterlessConstructorUsesCallingAssembly()
        {
            EmbeddedResourcesScriptLoader loader = new();

            await Assert.That(loader.ScriptFileExists("Resources/embedded.lua")).IsTrue();
        }
#endif
    }
}
