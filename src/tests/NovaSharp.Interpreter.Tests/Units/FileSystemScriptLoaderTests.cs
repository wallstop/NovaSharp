namespace NovaSharp.Interpreter.Tests.Units
{
#if DOTNET_CORE || (!(PCL || ENABLE_DOTNET || NETFX_CORE))
    using System;
    using System.IO;
    using NovaSharp.Interpreter.Loaders;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FileSystemScriptLoaderTests
    {
        [Test]
        public void ScriptFileExistsNormalizesWhitespaceAndSeparators()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);

            try
            {
                string filePath = Path.Combine(tempDirectory, "script.lua");
                File.WriteAllText(filePath, "return 1");

                FileSystemScriptLoader loader = new();

                char alternateSeparator = Path.DirectorySeparatorChar == '/' ? '\\' : '/';

                string userProvidedPath =
                    $" {filePath.Replace(Path.DirectorySeparatorChar, alternateSeparator)} ";

                Assert.That(loader.ScriptFileExists(userProvidedPath), Is.True);
            }
            finally
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }

        [Test]
        public void ScriptFileExistsThrowsWhenPathIsNull()
        {
            FileSystemScriptLoader loader = new();

            Assert.That(
                () => loader.ScriptFileExists(null!),
                Throws.TypeOf<ArgumentNullException>()
            );
        }
    }
#endif
}
