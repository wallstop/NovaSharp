namespace NovaSharp.Interpreter.Tests.TUnit.VM
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests.TUnit.TestInfrastructure;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

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

            FileNotFoundException exception = ExpectException<FileNotFoundException>(() =>
                Script.RunFile(path)
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

        private static string EncodeFunctionAsBase64(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            string base64 = Convert.ToBase64String(stream.ToArray());
            return StringModule.Base64DumpHeader + base64;
        }

        private static TException ExpectException<TException>(Action action)
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

            throw new InvalidOperationException(
                $"Expected exception of type {typeof(TException).Name}."
            );
        }
    }
}
