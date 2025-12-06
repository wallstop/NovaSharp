namespace NovaSharp.Interpreter.Tests.TUnit.Loaders
{
#if DOTNET_CORE || (!(PCL || ENABLE_DOTNET || NETFX_CORE))
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Loaders;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    public sealed class FileSystemScriptLoaderTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsNormalizesWhitespaceAndSeparators()
        {
            using TempDirectoryScope tempDirectoryScope = TempDirectoryScope.Create();
            string tempDirectory = tempDirectoryScope.DirectoryPath;
            string filePath = Path.Combine(tempDirectory, "script.lua");
            await File.WriteAllTextAsync(filePath, "return 1").ConfigureAwait(false);

            FileSystemScriptLoader loader = new();
            char alternateSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';
            string userProvidedPath =
                $" {filePath.Replace(Path.DirectorySeparatorChar, alternateSeparator)} ";

            await Assert.That(loader.ScriptFileExists(userProvidedPath)).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task ScriptFileExistsThrowsWhenPathIsNull()
        {
            FileSystemScriptLoader loader = new();

            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            {
                loader.ScriptFileExists(null);
            });

            await Assert.That(exception).IsNotNull();
        }
    }
#endif
}
