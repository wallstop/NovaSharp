namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Text;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Modules;
    using NovaSharp.Interpreter.Platforms;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LimitedPlatformAccessorTests
    {
        [Test]
        public void GetEnvironmentVariableReturnsNull()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();
            Assert.That(accessor.GetEnvironmentVariable("PATH"), Is.Null);
        }

        [Test]
        public void FilterSupportedCoreModulesRemovesIoAndOsSystem()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();

            CoreModules requested =
                CoreModules.Math | CoreModules.Io | CoreModules.OsSystem | CoreModules.Table;
            CoreModules filtered = accessor.FilterSupportedCoreModules(requested);

            Assert.That(filtered.Has(CoreModules.Math), Is.True);
            Assert.That(filtered.Has(CoreModules.Table), Is.True);
            Assert.That(filtered.Has(CoreModules.Io), Is.False);
            Assert.That(filtered.Has(CoreModules.OsSystem), Is.False);
        }

        [Test]
        public void UnsupportedOperationsThrowNotImplemented()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();
            Script script = new Script(CoreModules.None);

            Assert.Multiple(() =>
            {
                Assert.That(
                    () => accessor.OpenFile(script, "file.txt", Encoding.UTF8, "r"),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(
                    () => accessor.GetStandardStream(StandardFileType.StdIn),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(
                    () => accessor.GetTempFileName(),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(() => accessor.ExitFast(0), Throws.TypeOf<NotImplementedException>());
                Assert.That(
                    () => accessor.DeleteFile("file.txt"),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(
                    () => accessor.MoveFile("a.txt", "b.txt"),
                    Throws.TypeOf<NotImplementedException>()
                );
                Assert.That(
                    () => accessor.ExecuteCommand("echo hello"),
                    Throws.TypeOf<NotImplementedException>()
                );
            });
        }

        [Test]
        public void PlatformNamePrefixIsLimited()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();
            Assert.That(accessor.GetPlatformNamePrefix(), Is.EqualTo("limited"));
        }

        [Test]
        public void FileExistsThrowsNotImplemented()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();
            Assert.That(
                () => accessor.FileExists("file.txt"),
                Throws.TypeOf<NotImplementedException>()
            );
        }

        [Test]
        public void DefaultPrintDoesNotThrow()
        {
            LimitedPlatformAccessor accessor = new LimitedPlatformAccessor();
            Assert.That(() => accessor.DefaultPrint("hello limited platform"), Throws.Nothing);
        }
    }
}
