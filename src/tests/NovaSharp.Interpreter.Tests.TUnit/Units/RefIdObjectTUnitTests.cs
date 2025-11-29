#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class RefIdObjectTUnitTests
    {
        private static readonly Regex RefPattern = new(
            @"^Sample:\s[A-F0-9]{8}$",
            RegexOptions.Compiled
        );

        private sealed class SampleRefObject : RefIdObject { }

        [global::TUnit.Core.Test]
        public async Task ReferenceIdMonotonicallyIncreases()
        {
            SampleRefObject first = new();
            SampleRefObject second = new();

            await Assert.That(second.ReferenceId > first.ReferenceId).IsTrue();
        }

        [global::TUnit.Core.Test]
        public async Task FormatTypeStringAppendsHexSuffix()
        {
            SampleRefObject instance = new();

            string formatted = instance.FormatTypeString("Sample");

            await Assert.That(RefPattern.IsMatch(formatted)).IsTrue();
        }
    }
}
#pragma warning restore CA2007
