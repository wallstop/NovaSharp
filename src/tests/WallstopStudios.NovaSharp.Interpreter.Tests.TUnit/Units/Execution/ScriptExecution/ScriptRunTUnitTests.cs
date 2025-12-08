namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [PlatformDetectorIsolation]
    public sealed class ScriptRunTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task RunStringExecutesCodeWithDefaultScript()
        {
            DynValue result = Script.RunString("return 123");

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(123);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileExecutesFileContents()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFileScope = TempFileScope.Create(
                namePrefix: "script-",
                extension: ".lua"
            );
            string tempFile = tempFileScope.FilePath;

            await File.WriteAllTextAsync(tempFile, "return 'file-result'").ConfigureAwait(false);

            DynValue result = Script.RunFile(tempFile);

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("file-result");
        }

        [global::TUnit.Core.Test]
        public async Task RunFileThrowsWhenFileMissing()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope missingFileScope = TempFileScope.Create(
                extension: ".lua",
                createFile: false
            );
            string path = missingFileScope.FilePath;

            // Diagnostic context for debugging cross-platform issues
            string loaderType = Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "null";
            string diagnosticContext =
                $"Path: {path}, FileExists: {File.Exists(path)}, ScriptLoader: {loaderType}";

            FileNotFoundException exception = ExpectException<FileNotFoundException>(
                () => Script.RunFile(path),
                diagnosticContext
            );

            await Assert.That(exception.FileName).IsEqualTo(path);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileThrowsWhenDirectoryMissing()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempDirectoryScope tempDirScope = TempDirectoryScope.Create();

            // Use the created temp directory as a base, then add a non-existent nested path
            string nonExistentDirectory = Path.Combine(
                tempDirScope.DirectoryPath,
                "nonexistent",
                "nested",
                "script.lua"
            );

            // FileSystemScriptLoader throws FileNotFoundException (from FileStream)
            // when the directory doesn't exist, not DirectoryNotFoundException
            Exception exception = ExpectException<Exception>(
                () => Script.RunFile(nonExistentDirectory),
                $"Path: {nonExistentDirectory}"
            );

            // Accept either FileNotFoundException or DirectoryNotFoundException
            // as both are valid for "file in non-existent directory" scenarios
            await Assert.That(exception).IsAssignableTo<IOException>().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task DoFileWithExplicitLoaderThrowsWhenFileMissing()
        {
            using TempFileScope missingFileScope = TempFileScope.Create(
                extension: ".lua",
                createFile: false
            );
            string path = missingFileScope.FilePath;

            ScriptOptions options = new() { ScriptLoader = new FileSystemScriptLoader() };
            Script script = new(options);

            FileNotFoundException exception = ExpectException<FileNotFoundException>(
                () => script.DoFile(path),
                $"Path: {path}, Using explicit FileSystemScriptLoader"
            );

            await Assert.That(exception.FileName).IsEqualTo(path);
        }

        [global::TUnit.Core.Test]
        public async Task RunStringExecutesBase64Dump()
        {
            Script script = new();
            DynValue chunk = script.LoadString("return 77");

            string encoded = EncodeFunctionAsBase64(script, chunk);
            DynValue result = Script.RunString(encoded);

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        [Arguments("return 42", DataType.Number, 42.0, null)]
        [Arguments("return 'hello'", DataType.String, null, "hello")]
        [Arguments("return true", DataType.Boolean, null, null)]
        [Arguments("return nil", DataType.Nil, null, null)]
        [Arguments("return 1 + 2 * 3", DataType.Number, 7.0, null)]
        [Arguments("return 'foo' .. 'bar'", DataType.String, null, "foobar")]
        public async Task RunStringReturnsExpectedValue(
            string code,
            DataType expectedType,
            double? expectedNumber,
            string expectedString
        )
        {
            DynValue result = Script.RunString(code);

            await Assert.That(result.Type).IsEqualTo(expectedType).ConfigureAwait(false);

            if (expectedNumber.HasValue)
            {
                await Assert
                    .That(result.Number)
                    .IsEqualTo(expectedNumber.Value)
                    .ConfigureAwait(false);
            }

            if (expectedString != null)
            {
                await Assert.That(result.String).IsEqualTo(expectedString).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        [Arguments("return 42", DataType.Number, 42.0, null)]
        [Arguments("return 'hello'", DataType.String, null, "hello")]
        [Arguments("return true", DataType.Boolean, null, null)]
        [Arguments("return nil", DataType.Nil, null, null)]
        public async Task RunFileReturnsExpectedValue(
            string code,
            DataType expectedType,
            double? expectedNumber,
            string expectedString
        )
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFileScope = TempFileScope.CreateWithText(
                code,
                extension: ".lua"
            );

            DynValue result = Script.RunFile(tempFileScope.FilePath);

            await Assert.That(result.Type).IsEqualTo(expectedType).ConfigureAwait(false);

            if (expectedNumber.HasValue)
            {
                await Assert
                    .That(result.Number)
                    .IsEqualTo(expectedNumber.Value)
                    .ConfigureAwait(false);
            }

            if (expectedString != null)
            {
                await Assert.That(result.String).IsEqualTo(expectedString).ConfigureAwait(false);
            }
        }

        [global::TUnit.Core.Test]
        public async Task RunFileVerifiesScriptLoaderIsFileSystemLoader()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();

            // Verify the script loader was correctly set
            await Assert
                .That(Script.DefaultOptions.ScriptLoader)
                .IsTypeOf<FileSystemScriptLoader>()
                .ConfigureAwait(false);
        }

        private static string EncodeFunctionAsBase64(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            string base64 = Convert.ToBase64String(stream.ToArray());
            return StringModule.Base64DumpHeader + base64;
        }

        private static TException ExpectException<TException>(Action action, string context = null)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                string contextInfo = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
                throw new InvalidOperationException(
                    $"Expected exception of type {typeof(TException).Name} but got {ex.GetType().Name}: {ex.Message}.{contextInfo}",
                    ex
                );
            }

            string noExceptionContext = string.IsNullOrEmpty(context) ? "" : $" Context: {context}";
            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name} but no exception was thrown.{noExceptionContext}"
            );
        }
    }
}
