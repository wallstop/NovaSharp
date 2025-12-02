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
            await Assert.That(_framework.IsDbNull(DBNull.Value)).IsTrue().ConfigureAwait(false);
            await Assert.That(_framework.IsDbNull(null)).IsFalse().ConfigureAwait(false);
            await Assert.That(_framework.IsDbNull(new object())).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task StringContainsCharHandlesNullAndMissingCharacters()
        {
            await Assert
                .That(_framework.StringContainsChar("abc", 'b'))
                .IsTrue()
                .ConfigureAwait(false);
            await Assert
                .That(_framework.StringContainsChar("abc", 'z'))
                .IsFalse()
                .ConfigureAwait(false);
            await Assert
                .That(_framework.StringContainsChar(null, 'a'))
                .IsFalse()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetInterfaceUsesTypeInfoLookup()
        {
            const string interfaceName = "System.IDisposable";

            await Assert
                .That(_framework.GetInterface(typeof(DisposableFixture), interfaceName))
                .IsEqualTo(typeof(IDisposable))
                .ConfigureAwait(false);

            await Assert
                .That(_framework.GetInterface(typeof(DisposableFixture), "System.ICloneable"))
                .IsNull()
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task GetTypeInfoFromTypeReturnsTypeInfoInstance()
        {
            TypeInfo info = _framework.GetTypeInfoFromType(typeof(string));

            await Assert.That(info).IsEqualTo(typeof(string).GetTypeInfo()).ConfigureAwait(false);
        }
    }
}
