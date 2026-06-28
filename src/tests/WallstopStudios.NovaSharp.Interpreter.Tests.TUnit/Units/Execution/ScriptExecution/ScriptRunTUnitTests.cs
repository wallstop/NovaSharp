namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Units.Execution.ScriptExecution
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.CoreLib;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Loaders;
    using WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    [PlatformDetectorIsolation]
    [ScriptDefaultOptionsIsolation]
    public sealed class ScriptRunTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RunStringExecutesCodeWithDefaultScript(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue result = script.DoString("return 123");

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

            const string expectedCode = "return 'file-result'";

            // Write file using standard async file write
            await File.WriteAllTextAsync(tempFile, expectedCode).ConfigureAwait(false);

            // Verify the file was written correctly before executing
            bool fileExists = File.Exists(tempFile);
            string actualFileContents = fileExists
                ? await File.ReadAllTextAsync(tempFile).ConfigureAwait(false)
                : "<file not found>";
            long fileLength = fileExists ? new FileInfo(tempFile).Length : -1;
            string defaultLoaderType =
                Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "<null loader>";

            // Create a script directly to check the actual loader used
            Script testScript = new();
            string actualScriptLoaderType =
                testScript.Options.ScriptLoader?.GetType().Name ?? "<null loader>";

            // Diagnostic context for troubleshooting CI failures
            string diagnostics =
                $"FilePath={tempFile}, Exists={fileExists}, Length={fileLength}, "
                + $"Contents='{actualFileContents}', DefaultLoader={defaultLoaderType}, "
                + $"ActualScriptLoader={actualScriptLoaderType}";

            DynValue result = Script.RunFile(tempFile);

            // Use diagnostic message in assertion failure
            await Assert
                .That(result.Type)
                .IsEqualTo(DataType.String)
                .Because(
                    $"Expected String result from script. Actual type: {result.Type}. Diagnostics: {diagnostics}"
                )
                .ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("file-result").ConfigureAwait(false);
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
        [AllLuaVersions]
        public async Task DoFileWithExplicitLoaderThrowsWhenFileMissing(
            LuaCompatibilityVersion version
        )
        {
            using TempFileScope missingFileScope = TempFileScope.Create(
                extension: ".lua",
                createFile: false
            );
            string path = missingFileScope.FilePath;

            ScriptOptions options = new(Script.DefaultOptions)
            {
                ScriptLoader = new FileSystemScriptLoader(),
                CompatibilityVersion = version,
            };
            Script script = new(options);

            FileNotFoundException exception = ExpectException<FileNotFoundException>(
                () => script.DoFile(path),
                $"Path: {path}, Using explicit FileSystemScriptLoader"
            );

            await Assert.That(exception.FileName).IsEqualTo(path);
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public async Task RunStringExecutesBase64Dump(LuaCompatibilityVersion version)
        {
            Script script = new(version);
            DynValue chunk = script.LoadString("return 77");

            string encoded = EncodeFunctionAsBase64(script, chunk);
            DynValue result = script.DoString(encoded);

            await Assert.That(result.Type).IsEqualTo(DataType.Number);
            await Assert.That(result.Number).IsEqualTo(77);
        }

        [global::TUnit.Core.Test]
        [LuaTestMatrix(
            new object[] { "return 42", DataType.Number, 42.0, null },
            new object[] { "return 'hello'", DataType.String, null, "hello" },
            new object[] { "return true", DataType.Boolean, null, null },
            new object[] { "return nil", DataType.Nil, null, null },
            new object[] { "return 1 + 2 * 3", DataType.Number, 7.0, null },
            new object[] { "return 'foo' .. 'bar'", DataType.String, null, "foobar" }
        )]
        public async Task RunStringReturnsExpectedValue(
            LuaCompatibilityVersion version,
            string code,
            DataType expectedType,
            double? expectedNumber,
            string expectedString
        )
        {
            Script script = new(version);
            DynValue result = script.DoString(code);

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

        [global::TUnit.Core.Test]
        public async Task RunFileHandlesEmptyFile()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFileScope = TempFileScope.CreateEmpty(extension: ".lua");

            DynValue result = Script.RunFile(tempFileScope.FilePath);

            // Empty file returns Void (no return statement)
            await Assert.That(result.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileHandlesWhitespaceOnlyFile()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFileScope = TempFileScope.CreateWithText(
                "   \n\t\n   ",
                extension: ".lua"
            );

            DynValue result = Script.RunFile(tempFileScope.FilePath);

            // Whitespace-only file returns Void (no return statement)
            await Assert.That(result.Type).IsEqualTo(DataType.Void).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileHandlesUtf8BomFile()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();

            // Create file with UTF-8 BOM
            using TempFileScope tempFileScope = TempFileScope.CreateWithText(
                "return 'with-bom'",
                encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                extension: ".lua"
            );

            DynValue result = Script.RunFile(tempFileScope.FilePath);

            await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
            await Assert.That(result.String).IsEqualTo("with-bom").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileHandlesMultiStatementScript()
        {
            using PlatformDetectorOverrideScope platformScope =
                PlatformDetectionTestHelper.ForceFileSystemLoader();
            using TempFileScope tempFileScope = TempFileScope.CreateWithText(
                "local x = 10\nlocal y = 20\nreturn x + y",
                extension: ".lua"
            );

            DynValue result = Script.RunFile(tempFileScope.FilePath);

            await Assert.That(result.Type).IsEqualTo(DataType.Number).ConfigureAwait(false);
            await Assert.That(result.Number).IsEqualTo(30).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task RunFileScriptLoaderIsolationWorks()
        {
            // Verify the ForceFileSystemLoader scope properly isolates
            string loaderBeforeScope =
                Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "<null>";

            using (
                PlatformDetectorOverrideScope platformScope =
                    PlatformDetectionTestHelper.ForceFileSystemLoader()
            )
            {
                string loaderDuringScope =
                    Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "<null>";
                await Assert
                    .That(loaderDuringScope)
                    .IsEqualTo(nameof(FileSystemScriptLoader))
                    .ConfigureAwait(false);
            }

            // After scope ends, loader should be restored
            string loaderAfterScope =
                Script.DefaultOptions.ScriptLoader?.GetType().Name ?? "<null>";
            await Assert.That(loaderAfterScope).IsEqualTo(loaderBeforeScope).ConfigureAwait(false);
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
