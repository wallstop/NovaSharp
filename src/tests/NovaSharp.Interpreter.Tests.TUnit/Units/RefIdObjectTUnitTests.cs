namespace NovaSharp.Interpreter.Tests.TUnit.Units
{
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using global::TUnit.Core;
    using NovaSharp.Interpreter.DataTypes;

    public sealed class RefIdObjectTUnitTests
    {
        private const int MonotonicSampleCount = 10_000;

        private static readonly Regex RefPattern = new(
            @"^Sample:\s[A-F0-9]{8}$",
            RegexOptions.Compiled
        );

        private sealed class SampleRefObject : RefIdObject { }

        /// <summary>
        /// This test must run in isolation because RefIdObject uses a non-thread-safe
        /// counter that can produce non-monotonic IDs when other tests create RefIdObjects
        /// concurrently. See the class documentation for RefIdObject.
        /// </summary>
        [Test]
        [NotInParallel]
        public async Task ReferenceIdMonotonicallyIncreases()
        {
            // Create a large sample of RefIdObjects and verify IDs are strictly increasing
            SampleRefObject[] objects = new SampleRefObject[MonotonicSampleCount];
            for (int i = 0; i < MonotonicSampleCount; i++)
            {
                objects[i] = new SampleRefObject();
            }

            // Verify all reference IDs are strictly increasing
            int violations = 0;
            for (int i = 1; i < MonotonicSampleCount; i++)
            {
                if (objects[i].ReferenceId <= objects[i - 1].ReferenceId)
                {
                    violations++;
                }
            }

            await Assert.That(violations).IsEqualTo(0).ConfigureAwait(false);

            // Verify total advancement
            await Assert
                .That(objects[MonotonicSampleCount - 1].ReferenceId)
                .IsGreaterThan(objects[0].ReferenceId)
                .ConfigureAwait(false);
        }

        [Test]
        public async Task FormatTypeStringAppendsHexSuffix()
        {
            SampleRefObject instance = new();

            string formatted = instance.FormatTypeString("Sample");

            await Assert.That(RefPattern.IsMatch(formatted)).IsTrue().ConfigureAwait(false);
        }
    }
}
