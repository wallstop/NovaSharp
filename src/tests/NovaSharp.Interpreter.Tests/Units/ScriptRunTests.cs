namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.IO;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.CoreLib;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptRunTests
    {
        [Test]
        public void RunStringExecutesCodeWithDefaultScript()
        {
            DynValue result = Script.RunString("return 123");

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(123));
        }

        [Test]
        public void RunFileExecutesFileContents()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, "return 'file-result'");

                DynValue result = Script.RunFile(tempFile);

                Assert.That(result.Type, Is.EqualTo(DataType.String));
                Assert.That(result.String, Is.EqualTo("file-result"));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void RunFileThrowsWhenFileMissing()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.lua");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Assert.That(
                () => Script.RunFile(path),
                Throws.TypeOf<FileNotFoundException>().With.Property("FileName").EqualTo(path)
            );
        }

        [Test]
        public void RunStringExecutesBase64Dump()
        {
            Script producer = new();
            DynValue chunk = producer.LoadString("return 77");

            string encoded = EncodeFunctionAsBase64(producer, chunk);

            DynValue result = Script.RunString(encoded);

            Assert.That(result.Type, Is.EqualTo(DataType.Number));
            Assert.That(result.Number, Is.EqualTo(77));
        }

        private static string EncodeFunctionAsBase64(Script script, DynValue chunk)
        {
            using MemoryStream stream = new();
            script.Dump(chunk, stream);
            string base64 = Convert.ToBase64String(stream.ToArray());
            return StringModule.Base64DumpHeader + base64;
        }
    }
}
