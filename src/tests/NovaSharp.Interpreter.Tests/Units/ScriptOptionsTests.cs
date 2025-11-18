namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
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

        [Test]
        public void CallRecordsExecutionCounterWhenEnabled()
        {
            Script script = new();
            script.PerformanceStats.Enabled = true;

            script.DoString("function add(a, b) return a + b end");
            DynValue result = script.Call(
                script.Globals.Get("add"),
                DynValue.NewNumber(2),
                DynValue.NewNumber(3)
            );

            Assert.That(result.Number, Is.EqualTo(5));

            PerformanceResult stats = script.PerformanceStats.GetPerformanceCounterResult(
                PerformanceCounter.Execution
            );

            Assert.That(stats, Is.Not.Null);
            Assert.That(stats.Instances, Is.GreaterThanOrEqualTo(1));
        }
    }
}
