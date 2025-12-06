namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Platforms
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Interpreter.Platforms;

    public sealed class LimitedPlatformAccessorTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task GetEnvironmentVariableReturnsNull()
        {
            LimitedPlatformAccessor accessor = new();

            string value = accessor.GetEnvironmentVariable("PATH");

            await Assert.That(value).IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task FilterSupportedCoreModulesRemovesIoAndOsSystem()
        {
            LimitedPlatformAccessor accessor = new();
            CoreModules requested =
                CoreModules.Math | CoreModules.Io | CoreModules.OsSystem | CoreModules.Table;

            CoreModules filtered = accessor.FilterSupportedCoreModules(requested);

            await Assert.That(filtered.Has(CoreModules.Math)).IsTrue();
            await Assert.That(filtered.Has(CoreModules.Table)).IsTrue();
            await Assert.That(filtered.Has(CoreModules.Io)).IsFalse();
            await Assert.That(filtered.Has(CoreModules.OsSystem)).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task UnsupportedOperationsThrowNotImplemented()
        {
            LimitedPlatformAccessor accessor = new();
            Script script = new(CoreModules.Basic | CoreModules.GlobalConsts);

            NotImplementedException open = Assert.Throws<NotImplementedException>(() =>
                accessor.OpenFile(script, "file.txt", Encoding.UTF8, "r")
            );
            NotImplementedException std = Assert.Throws<NotImplementedException>(() =>
                accessor.GetStandardStream(StandardFileType.StdIn)
            );
            NotImplementedException temp = Assert.Throws<NotImplementedException>(() =>
                accessor.GetTempFileName()
            );
            NotImplementedException exit = Assert.Throws<NotImplementedException>(() =>
                accessor.ExitFast(0)
            );
            NotImplementedException delete = Assert.Throws<NotImplementedException>(() =>
                accessor.DeleteFile("file.txt")
            );
            NotImplementedException move = Assert.Throws<NotImplementedException>(() =>
                accessor.MoveFile("a.txt", "b.txt")
            );
            NotImplementedException execute = Assert.Throws<NotImplementedException>(() =>
                accessor.ExecuteCommand("echo hello")
            );

            await Assert.That(open).IsNotNull();
            await Assert.That(std).IsNotNull();
            await Assert.That(temp).IsNotNull();
            await Assert.That(exit).IsNotNull();
            await Assert.That(delete).IsNotNull();
            await Assert.That(move).IsNotNull();
            await Assert.That(execute).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public async Task PlatformNamePrefixIsLimited()
        {
            LimitedPlatformAccessor accessor = new();

            string prefix = accessor.GetPlatformNamePrefix();

            await Assert.That(prefix).IsEqualTo("limited");
        }

        [global::TUnit.Core.Test]
        public async Task FileExistsThrowsNotImplemented()
        {
            LimitedPlatformAccessor accessor = new();

            NotImplementedException exception = Assert.Throws<NotImplementedException>(() =>
                accessor.FileExists("file.txt")
            );

            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public void DefaultPrintDoesNotThrow()
        {
            LimitedPlatformAccessor accessor = new();

            accessor.DefaultPrint("hello limited platform");
        }
    }
}
