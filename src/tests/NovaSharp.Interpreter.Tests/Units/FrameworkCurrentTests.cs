namespace NovaSharp.Interpreter.Tests.Units
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using NovaSharp.Interpreter.Compatibility.Frameworks;
    using NUnit.Framework;

    [TestFixture]
    public sealed class FrameworkCurrentTests
    {
#if DOTNET_CORE
        [Test]
        public void GetInterfaceReturnsRequestedInterface()
        {
            FrameworkCurrent framework = new();
            Type result = framework.GetInterface(typeof(List<int>), "IEnumerable`1");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("IEnumerable`1"));
        }

        [Test]
        public void GetTypeInfoFromTypeReturnsTypeInfo()
        {
            FrameworkCurrent framework = new();
            TypeInfo info = framework.GetTypeInfoFromType(typeof(string));
            Assert.That(info, Is.Not.Null);
            Assert.That(info.FullName, Is.EqualTo(typeof(string).FullName));
        }

        [Test]
        public void IsDbNullDetectsDBNullInstances()
        {
            FrameworkCurrent framework = new();
            Assert.That(framework.IsDbNull(DBNull.Value), Is.True);
            Assert.That(framework.IsDbNull(null), Is.False);
            Assert.That(framework.IsDbNull("value"), Is.False);
        }

        [Test]
        public void StringContainsCharUsesOrdinalSearch()
        {
            FrameworkCurrent framework = new();
            Assert.That(framework.StringContainsChar("nova", 'o'), Is.True);
            Assert.That(framework.StringContainsChar("nova", 'x'), Is.False);
        }
#else
        [Test]
        public void FrameworkTestsSkippedOnNonCoreRuntimes()
        {
            Assert.Ignore("FrameworkCurrent is only compiled when DOTNET_CORE is defined.");
        }
#endif
    }
}
