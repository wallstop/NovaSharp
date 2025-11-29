#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.Compatibility.Frameworks;

    public sealed class FrameworkCurrentTUnitTests
    {
        private readonly FrameworkCurrent _framework = new();

        static FrameworkCurrentTUnitTests()
        {
            new DisposableFixture().Dispose();
        }

        private sealed class DisposableFixture : IDisposable
        {
            public void Dispose() { }
        }

        [global::TUnit.Core.Test]
        public async Task IsDbNullReturnsTrueOnlyForDbNullInstances()
        {
            await Assert.That(_framework.IsDbNull(DBNull.Value)).IsTrue();
            await Assert.That(_framework.IsDbNull(null)).IsFalse();
            await Assert.That(_framework.IsDbNull(new object())).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task StringContainsCharHandlesNullAndMissingCharacters()
        {
            await Assert.That(_framework.StringContainsChar("abc", 'b')).IsTrue();
            await Assert.That(_framework.StringContainsChar("abc", 'z')).IsFalse();
            await Assert.That(_framework.StringContainsChar(null, 'a')).IsFalse();
        }

        [global::TUnit.Core.Test]
        public async Task GetInterfaceUsesTypeInfoLookup()
        {
            const string interfaceName = "System.IDisposable";

            await Assert
                .That(_framework.GetInterface(typeof(DisposableFixture), interfaceName))
                .IsEqualTo(typeof(IDisposable));

            await Assert
                .That(_framework.GetInterface(typeof(DisposableFixture), "System.ICloneable"))
                .IsNull();
        }

        [global::TUnit.Core.Test]
        public async Task GetTypeInfoFromTypeReturnsTypeInfoInstance()
        {
            TypeInfo info = _framework.GetTypeInfoFromType(typeof(string));

            await Assert.That(info).IsEqualTo(typeof(string).GetTypeInfo());
        }
    }
}
#pragma warning restore CA2007
