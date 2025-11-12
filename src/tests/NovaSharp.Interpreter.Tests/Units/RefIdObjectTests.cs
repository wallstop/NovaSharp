namespace NovaSharp.Interpreter.Tests.Units
{
    using System.Text.RegularExpressions;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RefIdObjectTests
    {
        private static readonly Regex RefPattern = new(@"^Sample:\s[A-F0-9]{8}$", RegexOptions.Compiled);

        private sealed class SampleRefObject : RefIdObject { }

        [Test]
        public void ReferenceIdMonotonicallyIncreases()
        {
            SampleRefObject first = new();
            SampleRefObject second = new();

            Assert.That(second.ReferenceId, Is.GreaterThan(first.ReferenceId));
        }

        [Test]
        public void FormatTypeStringAppendsHexSuffix()
        {
            SampleRefObject instance = new();

            string formatted = instance.FormatTypeString("Sample");

            Assert.That(formatted, Does.Match(RefPattern));
        }
    }
}
