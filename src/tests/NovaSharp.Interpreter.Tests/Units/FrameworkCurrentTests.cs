namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Reflection;
    using NovaSharp.Interpreter.Compatibility.Frameworks;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FrameworkCurrentTests
    {
        private readonly FrameworkCurrent _framework = new();

        static FrameworkCurrentTests()
        {
            new DisposableFixture().Dispose();
        }

        private sealed class DisposableFixture : IDisposable
        {
            public void Dispose() { }
        }

        [Test]
        public void IsDbNullReturnsTrueOnlyForDbNullInstances()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_framework.IsDbNull(DBNull.Value), Is.True);
                Assert.That(_framework.IsDbNull(null), Is.False);
                Assert.That(_framework.IsDbNull(new object()), Is.False);
            });
        }

        [Test]
        public void StringContainsCharHandlesNullAndMissingCharacters()
        {
            Assert.Multiple(() =>
            {
                Assert.That(_framework.StringContainsChar("abc", 'b'), Is.True);
                Assert.That(_framework.StringContainsChar("abc", 'z'), Is.False);
                Assert.That(_framework.StringContainsChar(null, 'a'), Is.False);
            });
        }

        [Test]
        public void GetInterfaceUsesTypeInfoLookup()
        {
            const string interfaceName = "System.IDisposable";

            Assert.That(
                _framework.GetInterface(typeof(DisposableFixture), interfaceName),
                Is.EqualTo(typeof(IDisposable))
            );

            Assert.That(
                _framework.GetInterface(typeof(DisposableFixture), "System.ICloneable"),
                Is.Null
            );
        }

        [Test]
        public void GetTypeInfoFromTypeReturnsTypeInfoInstance()
        {
            TypeInfo info = _framework.GetTypeInfoFromType(typeof(string));

            Assert.That(info, Is.EqualTo(typeof(string).GetTypeInfo()));
        }
    }
}
