namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.Diagnostics;
    using NovaSharp.Interpreter.Tests.TestUtilities;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ScriptOptionsTests
    {
        [Test]
        public void HighResolutionClockFlowsIntoPerformanceStatistics()
        {
            FakeHighResolutionClock clock = new();
            ScriptOptions options = new ScriptOptions(Script.DefaultOptions)
            {
                HighResolutionClock = clock,
            };

            Script script = new(options);
            script.PerformanceStats.Enabled = true;

            using (script.PerformanceStats.StartStopwatch(PerformanceCounter.Execution))
            {
                clock.AdvanceMilliseconds(12);
            }

            PerformanceResult result = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Counter, Is.EqualTo(12));
        }
    }
}
